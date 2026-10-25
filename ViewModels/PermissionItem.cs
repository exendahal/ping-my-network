using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// One thing the app asks for, why it needs it, and whether it has it.
    /// </summary>
    public sealed partial class PermissionItem : ObservableObject
    {
        #region Variables

        private readonly Func<Task<bool>> _Check;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <param name="icon">Icon glyph.</param>
        /// <param name="title">What is asked for, e.g. "Location".</param>
        /// <param name="body">What the app does with it.</param>
        /// <param name="actionText">The button, e.g. "Allow" or "Turn on".</param>
        /// <param name="check">Whether it is granted now.</param>
        /// <param name="request">Asks for it, or opens the system page for it.</param>
        /// <param name="grantedText">Shown once granted; "Allowed" when null.</param>
        public PermissionItem(string icon, string title, string body, string actionText, Func<Task<bool>> check, Func<Task> request, string? grantedText = null)
        {
            GrantedText = grantedText ?? AppResources.PermissionAllowed;
            Icon = icon;
            Title = title;
            Body = body;
            ActionText = actionText;
            _Check = check;

            RequestCommand = new AsyncRelayCommand(async () =>
            {
                await request();
                await RefreshAsync();
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Icon glyph.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// What is asked for.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// What the app does with it.
        /// </summary>
        public string Body { get; }

        /// <summary>
        /// The button's label.
        /// </summary>
        public string ActionText { get; }

        /// <summary>
        /// Shown once granted, e.g. "Allowed" or "On".
        /// </summary>
        public string GrantedText { get; }

        /// <summary>
        /// Asks for it.
        /// </summary>
        public IAsyncRelayCommand RequestCommand { get; }

        /// <summary>
        /// Whether it is granted.
        /// </summary>
        [ObservableProperty]
        public partial bool IsGranted { get; set; }

        /// <summary>
        /// Whether to offer the button.
        /// </summary>
        [ObservableProperty]
        public partial bool IsMissing { get; set; } = true;

        /// <summary>
        /// The row read as one phrase by a screen reader.
        /// </summary>
        [ObservableProperty]
        public partial string SemanticDescription { get; set; } = string.Empty;

        #endregion

        #region Methods

        /// <summary>
        /// Checks again, e.g. after the user comes back from system settings.
        /// </summary>
        /// <returns>A task that completes once checked.</returns>
        public async Task RefreshAsync()
        {
            IsGranted = await _Check();
            IsMissing = !IsGranted;
            SemanticDescription = $"{Title}, {(IsGranted ? GrantedText : AppResources.PermissionNotAllowed)}";
        }

        #endregion
    }
}
