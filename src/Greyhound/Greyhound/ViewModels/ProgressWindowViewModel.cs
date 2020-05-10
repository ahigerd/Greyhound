using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greyhound
{
    /// <summary>
    /// Progress Window View Model
    /// </summary>
    internal class ProgressWindowViewModel : Notifiable
    {
        /// <summary>
        /// Gets or Sets the number of items
        /// </summary>
        public double Count
        {
            get
            {
                return GetValue<double>("Count");
            }
            set
            {
                SetValue(value, "Count");
            }
        }

        /// <summary>
        /// Gets or Sets the current progress value
        /// </summary>
        public double Value
        {
            get
            {
                return GetValue<double>("Value");
            }
            set
            {
                SetValue(value, "Value");
            }
        }

        /// <summary>
        /// Gets or Sets if the progress bar is Indeterminate
        /// </summary>
        public bool Indeterminate
        {
            get
            {
                return GetValue<bool>("Indeterminate");
            }
            set
            {
                SetValue(value, "Indeterminate");
            }
        }

        /// <summary>
        /// Gets or Sets the Display Text
        /// </summary>
        public string Text
        {
            get
            {
                return GetValue<string>("Text");
            }
            set
            {
                SetValue(value, "Text");
            }
        }
    }
}
