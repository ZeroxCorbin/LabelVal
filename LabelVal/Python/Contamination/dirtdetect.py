import os
import cv2
import numpy as np

version = "Dirt Detect V2.4"

currentfilename = "na"
output_folder_created = "na"

########
#version 2.1 settings pretty sensative
ThreshSize_v2p1=223
objsens_v2p1=3.5
ContrastUse_v2p1=False

########
#version 2.3 settings pretty sensative
ThreshSize_v2p3=33
objsens_v2p3=3.7
ContrastUse_v2p3=False

########
#version 2.4 settings less sensative
ThreshSize_v2p4=223
objsens_v2p4=6.5
clipLimit_v2p4=0.5  #contrast enhace
tileGridSize_v2p4=(32,32) #contrast enhance
ContrastUse_v2p4=True

########################
#### active settings ####
clipLimit=clipLimit_v2p4
tileGridSize=tileGridSize_v2p4
active_ThreshSize = 1 | ThreshSize_v2p4 # must be an odd size
active_objsens = objsens_v2p4
ContrastUse = ContrastUse_v2p4
Border = 8

######## debugging images ########
same_intermediate_images=False
#same_intermediate_images=True

revision_text = """ Revision Log:
V2.1 ------
    This was very sensitive to most disturbances. very little will escape but there are always examples of little things to go undetected but they are in the realm of not affecting image processing and unlikely to be noticed by customers.
    ThreshSize_v2p1=223
    objsens_v2p1=3.5
V2.3 ------
    tuned to be less agressive
    ThreshSize_v2p3=33
    objsens_v2p3=3.7
V2.4 ------
    tuned to be less agressive. added a contrast enhance to allow for a finer threshold tuning to be possible
    ThreshSize_v2p4=223
    objsens_v2p4=6.5
    clipLimit_v2p4=1.2
    tileGridSize_v2p4=(32,32)    
 """
def revision():
    print (revision_text)
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
#
def apply_Edge_smoothing(image):
    """Apply a 3x3 smoothing filter to the image."""
    kernel = np.ones((3, 3), np.float32) / 9
    smoothed = cv2.filter2D(image, -1, kernel)
    
    return smoothed
    
    
########################################################
#
#
#
#    
def apply_Strong_smoothing(image):
    """Apply a 3x3 smoothing filter to the image."""
    #kernel = np.ones((3, 3), np.float32) / 9
    """Apply a 5x5 smoothing filter to the image."""
    #kernel = np.ones((5, 5), np.float32) / 25
    """Apply a 9x9 smoothing filter to the image."""
    
    """
    kernel = np.ones((9, 9), np.float32) / 81
    smoothed = cv2.filter2D(image, -1, kernel)    
    result = cv2.addWeighted(image, 0.1, smoothed, 0.9, 0)
    """
    # Mix 50% of the smoothed image with 50% of the original image
    #result = cv2.addWeighted(image, 0.3, smoothed, 0.7, 0)
    #result = cv2.addWeighted(image, 0.5, smoothed, 0.5, 0)
    
    result = cv2.GaussianBlur(image, (9, 9), 6)
    return result
    
    
########################################################
#
#
#
#
def detect_weak_blobs(image):
    global currentfilename
    global output_folder_created
    global clipLimit, tileGridSize
    global ContrastUse
    """Detect weak blobs in the image which might be dirt on the sensor."""
    # Convert image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    
    ################
    # on a smoothed denoised image do a slight contrast enhance``
    # Create a CLAHE object``
    if ContrastUse == True:
        clahe = cv2.createCLAHE(clipLimit, tileGridSize)
        # Apply CLAHE to the image
        gray = clahe.apply(gray)    
    ###############
    # Save image for inspection
    if same_intermediate_images == True:
        theshimgname= os.path.join(output_folder_created, os.path.splitext(os.path.basename(currentfilename))[0] + '_4_Cont.png')
        cv2.imwrite(theshimgname, gray)
        
    #######################################
    ## Apply threshold to get binary image  50 255
    """
    Min = 5
    Max = 25
    _, thresh = cv2.threshold(gray, Min, Max, cv2.THRESH_BINARY_INV)
    ## Find contours (blobs)
    contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    """
    # ADAPTIVE_THRESH_MEAN_C  ADAPTIVE_THRESH_GAUSSIAN_C   THRESH_BINARY   THRESH_BINARY_INV
    # Invert the grayscale image
    #inverted_gray = cv2.bitwise_not(gray)

    ThreshSize=active_ThreshSize
    objsens=active_objsens
    # v2.1 --> th3 = cv2.adaptiveThreshold(gray,255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C,  cv2.THRESH_BINARY_INV,ThreshSize,objsens)
    th3 = cv2.adaptiveThreshold(gray,255,cv2.ADAPTIVE_THRESH_MEAN_C,  cv2.THRESH_BINARY_INV,ThreshSize,objsens)
    ###############
    # Save image for inspection
    if same_intermediate_images == True:
        theshimgname= os.path.join(output_folder_created, os.path.splitext(os.path.basename(currentfilename))[0] + '_5_thresh.png')
        cv2.imwrite(theshimgname, th3)
    
    #########################################
    # Define a kernel for erosion
    kernel = np.ones((3, 3), np.uint8)
    # Apply erosion to the thresholded image
    eroded_image = cv2.erode(th3, kernel, iterations=1)
    ###############
    # save image  for inspection
    if same_intermediate_images == True:
        theshimgname= os.path.join(output_folder_created, os.path.splitext(os.path.basename(currentfilename))[0] + '_6_erode.png')
        cv2.imwrite(theshimgname, eroded_image)
    
    
    #########################################
    # make the blob finds  
    contours, _ = cv2.findContours(eroded_image, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)     
    
    #######################################
    # Apply edge detection to get binary image
    """
    low = 50
    upper = low*4
    myapertureSize=5
    edges = cv2.Canny(gray, low, upper, apertureSize=myapertureSize)
    # Find contours (blobs)
    contours, _ = cv2.findContours(edges, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    """
    
    weak_blobs = []
    height, width = gray.shape
    border_margin = Border
    minblobsize = 5
    for contour in contours:
        if cv2.contourArea(contour) > minblobsize:  # Weak blob if area more tha a single spot
            # Check if the blob is within the 8-pixel margin from the border
            x, y, w, h = cv2.boundingRect(contour)
            if x > border_margin and y > border_margin and x + w < width - border_margin and y + h < height - border_margin:
                weak_blobs.append(contour)
    return weak_blobs


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
def process_image(image_path, output_folder):
    """Process a single image to find weak blobs and save results."""
    image = cv2.imread(image_path)
    

    # Generate and apply flat field correction
    flat_field_image = generate_flat_field(image)
    uncorrected_image = apply_flat_field_correction(image, flat_field_image)
    setbright=160
    setbright=100
    corrected_image = normalize(uncorrected_image, setbright)

    ###############
    # Save flat field image for inspection        
    if same_intermediate_images == True:
        flat_field_image_path = os.path.join(output_folder, os.path.splitext(os.path.basename(image_path))[0] + '_1_flat_field.png')
        cv2.imwrite(flat_field_image_path, flat_field_image)
    ################
    # Save flat field corrected image for inspection
    if same_intermediate_images == True:
        flat_field_image_path = os.path.join(output_folder, os.path.splitext(os.path.basename(image_path))[0] + '_2_flat_field_result.png')
        cv2.imwrite(flat_field_image_path, corrected_image)
        
    # Apply smoothing
    #smoothed1 = apply_Edge_smoothing(corrected_image)    
    smoothed1 = apply_Strong_smoothing(corrected_image)
    smoothed = apply_Edge_smoothing(smoothed1) 
    #smoothed = apply_Edge_smoothing(smoothed1)

    ################
    # Save smoothedimage for inspection
    if same_intermediate_images == True:
        flat_field_image_path = os.path.join(output_folder, os.path.splitext(os.path.basename(image_path))[0] + '_3_smoothed_result.png')
        cv2.imwrite(flat_field_image_path, smoothed)
    

    #### find dirt spots now
    blobs = detect_weak_blobs(smoothed)
    
    # Create output image with circles around detected blobs
    output_image = image.copy()
    base_name = os.path.splitext(os.path.basename(image_path))[0]
    
    with open(os.path.join(output_folder, base_name + '.txt'), 'w') as f:
        for blob in blobs:
            (x, y), radius = cv2.minEnclosingCircle(blob)
            center = (int(x), int(y))
            radius = int(radius)
            cv2.circle(output_image, center, radius, (0, 0, 255), 2)
            f.write(f'Blob found at: {center}, Radius: {radius}\n')

    if len(blobs) > 0:
        output_image_path = os.path.join(output_folder, base_name + '_result.png')
        cv2.imwrite(output_image_path, output_image)


########################################################
#
#
#
#
def process_folder(folder_path):
    global currentfilename
    global output_folder_created
    """Process all BMP and PNG images in the given folder."""
    output_folder = os.path.join(folder_path, 'output_dirt')
    os.makedirs(output_folder, exist_ok=True)
    
    output_folder_created = output_folder
    
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


    for filename in os.listdir(folder_path):
        if filename.lower().endswith(('.jpg', '.bmp', '.png')):
            image_path = os.path.join(folder_path, filename)
            currentfilename = image_path
            process_image(image_path, output_folder)


########################################################
#
#
#
#
if __name__ == "__main__":
    import sys
    
    print(version)
    
    if len(sys.argv) < 2:
        print("Usage: python3 dirtdetect.py <folder_path_of_images>")
        folder_path = "./"
    else:
        folder_path = sys.argv[1]
        if folder_path == "-log":
            revision()
            sys.exit()
    process_folder(folder_path)
