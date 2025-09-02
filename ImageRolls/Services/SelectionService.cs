using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LabelVal.ImageRolls.Services
{
    public class SelectionService
    {
        private List<WeakReference<ListView>> _listViews = new List<WeakReference<ListView>>();

        public void RegisterListView(ListView listView)
        {
            _listViews.Add(new WeakReference<ListView>(listView));
        }

        public void NotifySelectionChanged(ListView source)
        {
            foreach (var listViewRef in _listViews)
            {
                if (listViewRef.TryGetTarget(out var listView) && listView != source)
                {
                    listView.SelectedItem = null;
                }
            }
        }
    }
}
