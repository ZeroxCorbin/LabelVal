import os
import cv2
import numpy as np

version="Bad Pixels Detect V1.0"


def detect_bad_pixels(image, ref_image, threshold):
    bad_pixel_groups = []
    delta_img = ref_image.astype(np.int32) - image.astype(np.int32)
    rows, cols = image.shape
    upper_thresh = 128 + threshold
    lower_thresh = 128 - threshold

    for j in range(1, rows - 1):
        for i in range(1, cols - 1):
            bad_pix = 0
            ref_pix = ref_image[j, i] * 128
            pix = image[j, i]
            if pix == 0:
                pix = 1
            pix = ref_pix / pix
            if pix >= upper_thresh or pix <= lower_thresh:
                bad_pix += 1

            ref_pix = ref_image[j, i+1] * 128
            pix = image[j, i+1]
            if pix == 0:
                pix = 1
            pix = ref_pix / pix
            if pix >= upper_thresh or pix <= lower_thresh:
                bad_pix += 1

            ref_pix = ref_image[j+1, i] * 128
            pix = image[j+1, i]
            if pix == 0:
                pix = 1
            pix = ref_pix / pix
            if pix >= upper_thresh or pix <= lower_thresh:
                bad_pix += 1

            ref_pix = ref_image[j+1, i+1] * 128
            pix = image[j+1, i+1]
            if pix == 0:
                pix = 1
            pix = ref_pix / pix
            if pix >= upper_thresh or pix <= lower_thresh:
                bad_pix += 1

            if bad_pix >= 2:
                bad_pixel_groups.append((i, j))

    return bad_pixel_groups


########################################################
#
#
#
#
def generate_flat_field(image):
    """Generate a flat field image using a very low-pass filter."""
    flat_field = cv2.GaussianBlur(image, (101, 101), 0)
    #flat_field = cv2.GaussianBlur(image, (81, 81), 0)
    return flat_field

########################################################
#
#
#
#
def apply_flat_field_correction(image, flat_field_image):
    """Apply flat field correction to the image."""
    # Convert to float32 for division
    image_float = image.astype(np.float32)
    flat_field_float = flat_field_image.astype(np.float32)
    
    # Normalize the flat field image
    flat_field_normalized = flat_field_float / np.mean(flat_field_float)
    
    # Apply flat field correction
    corrected_image = image_float / flat_field_normalized
    corrected_image = np.clip(corrected_image, 0, 255)  # Clip values to valid range
    corrected_image = corrected_image.astype(np.uint8)
    return corrected_image


########################################################
#
#
#
# level = 0-255
def normalize(image, level):
    # Desired average value
    desired_mean = level 

    # Compute the current mean of the image
    current_mean = np.mean(image)

    # Compute the scaling factor
    scaling_factor = desired_mean / current_mean

    # Normalize the image by scaling pixel values
    normalized_image = image * scaling_factor

    # Clip the values to ensure they remain within the valid range [0, 255]
    normalized_image = np.clip(normalized_image, 0, 255).astype(np.uint8)
    return normalized_image

########################################################
#
#
#
#
def detect_bad_pixels_cv(image, ref_image, threshold):
    upper_thresh = threshold #128 + threshold
    lower_thresh = 1/threshold #128 - threshold

    # Calculate the relative pixel differences
    image = image.astype(np.float32)
    ref_image = ref_image.astype(np.float32)
    with np.errstate(divide='ignore', invalid='ignore'):
        ratio = ref_image / np.where(image == 0, 1, image)
    ratio[np.isnan(ratio)] = 0  # Replace NaNs caused by division by zero

    # Identify bad pixels
    bad_pixels = np.logical_or(ratio >= upper_thresh, ratio <= lower_thresh).astype(np.uint8)

    # Use a 2x2 kernel to find areas with 2 or more bad pixels
    kernel = np.ones((2, 2), np.uint8)
    convolved = cv2.filter2D(bad_pixels, -1, kernel)

    # Find locations with 2 or more bad pixels in 2x2 areas, skipping the border
    border_size = 16
    rows, cols = bad_pixels.shape
    bad_pixel_groups = np.argwhere(convolved[border_size:rows-border_size, border_size:cols-border_size] >= 2)
    # Adjust coordinates due to border offset
    bad_pixel_groups += border_size
    return bad_pixel_groups

########################################################
#
#
#
#
def process_images(input_folder, threshold):
    output_folder = os.path.join(input_folder, 'output_pix')
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)


    # Remove all files in the output folder
    for filename in os.listdir(output_folder):
        file_path = os.path.join(output_folder, filename)
        try:
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)  # Remove the file
            elif os.path.isdir(file_path):
                shutil.rmtree(file_path)  # Remove the directory and its contents
        except Exception as e:
            print(f'Failed to delete {file_path}. Reason: {e}')


    for filename in os.listdir(input_folder):
        if filename.endswith('.bmp') or filename.endswith('.png'):
            image_path = os.path.join(input_folder, filename)

            image = cv2.imread(image_path, cv2.IMREAD_GRAYSCALE)
            # Generate and apply flat field correction
            flat_field_image = generate_flat_field(image)
            uncorrected_image = apply_flat_field_correction(image, flat_field_image)
            setbright=160
            setbright=100
            corrected_image = normalize(uncorrected_image, setbright)

            ref_image = cv2.GaussianBlur(corrected_image, (9, 9), 6)
            bad_pixel_groups = detect_bad_pixels_cv(corrected_image, ref_image, threshold)

            report_path = os.path.join(output_folder, f"{os.path.splitext(filename)[0]}_badpix_report.txt")
            with open(report_path, 'w') as report_file:
                for (x, y) in bad_pixel_groups:
                    #report_file.write(f"Bad pixel group detected at X={x}, Y={y}\n")
                    report_file.write(f"Bad pixel group detected at X={y}, Y={x}\n")

            output_image = cv2.cvtColor(image, cv2.COLOR_GRAY2BGR)
            for (x, y) in bad_pixel_groups:
                #cv2.circle(output_image, (x, y), 16, (0, 0, 255), 2)
                cv2.circle(output_image, (y, x), 16, (0, 0, 255), 2)

            if len(bad_pixel_groups) > 0:
                output_image_path = os.path.join(output_folder, f"{os.path.splitext(filename)[0]}_badpix_output.png")
                cv2.imwrite(output_image_path, output_image)

if __name__ == "__main__":
    import sys
    #import argparse
    #parser = argparse.ArgumentParser(description="Detect bad pixel groups in images.")
    #parser.add_argument('input_folder', type=str, help="Input folder containing BMP or PNG images.")
    #parser.add_argument('--threshold', type=int, default=15, help="Threshold for bad pixel detection.")

    #args = parser.parse_args()

    print(version)
    
    if len(sys.argv) < 2:
        print("Usage: python3 dirtdetect.py <folder_path_of_images>")
        folder_path = "./"
    else:
        folder_path = sys.argv[1]

    process_images(folder_path, 1.13)
