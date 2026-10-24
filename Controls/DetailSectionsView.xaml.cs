using System.Collections;
using System.Windows.Input;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// Titled cards of label and value rows, shared by the network and device details screens.
    /// </summary>
    public partial class DetailSectionsView : ContentView
    {
        #region Variables

        /// <summary>
        /// The sections to show.
        /// </summary>
        public static readonly BindableProperty SectionsProperty = BindableProperty.Create(
            nameof(Sections),
            typeof(IEnumerable),
            typeof(DetailSectionsView),
            propertyChanged: OnSectionsChanged);

        /// <summary>
        /// Runs with the tapped row as its parameter.
        /// </summary>
        public static readonly BindableProperty CopyCommandProperty = BindableProperty.Create(
            nameof(CopyCommand),
            typeof(ICommand),
            typeof(DetailSectionsView));

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view.
        /// </summary>
        public DetailSectionsView()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The sections to show.
        /// </summary>
        public IEnumerable? Sections
        {
            get => (IEnumerable?)GetValue(SectionsProperty);
            set => SetValue(SectionsProperty, value);
        }

        /// <summary>
        /// Runs with the tapped row as its parameter.
        /// </summary>
        public ICommand? CopyCommand
        {
            get => (ICommand?)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Hands the sections to the layout.
        /// </summary>
        /// <param name="bindable">The view.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">The new sections.</param>
        private static void OnSectionsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            BindableLayout.SetItemsSource(((DetailSectionsView)bindable).SectionList, (IEnumerable?)newValue);
        }

        #endregion
    }
}
