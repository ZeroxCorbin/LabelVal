using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.WindowViewModels
{
    public class JobRunViewModel
    {


        private IDialogCoordinator dialogCoordinator;
        public JobRunViewModel(IDialogCoordinator diag)
        {
            dialogCoordinator = diag;

        }
    }
}
