#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DreamfireSubmodules.Editor.Services.Discovery;
using DreamfireSubmodules.Editor.Services.Git;
using DreamfireSubmodules.Editor.Services.GitLab;
using DreamfireSubmodules.Editor.Services.Packages;
using DreamfireSubmodules.Editor.Services.Updates;
using DreamfireSubmodules.Editor.Services.Versioning;
using DreamfireSubmodules.Editor.Windows;
using DreamfireSubmodules.Editor.Authentication;
using DreamfireSubmodules.Scripts.Authentication;
using DreamfireSubmodules.Scripts.DreamfireLibrary;
using DreamfireSubmodules.Scripts.DreamfireSubmoduleSettings;
using DreamfireSubmodules.Scripts.Services;
using DreamfireSubmodules.Scripts.Services.Discovery;
using DreamfireSubmodules.Scripts.Services.Git;
using DreamfireSubmodules.Scripts.Services.GitLab;
using DreamfireSubmodules.Scripts.Services.Packages;
using DreamfireSubmodules.Scripts.Services.Updates;
using UnityEditor;
using UnityEngine;
using DreamfireSubmodules.Scripts.ServiceResult;

namespace DreamfireSubmodules.Editor
{
    public sealed class DreamfireLibraryManagerWindow : EditorWindow
    {
        private const string SettingsFilter =
            "t:DreamfireSubmoduleSettings";

        private readonly List<DreamfireLibraryState> _libraries =
            new();

        private readonly List<DreamfireSubmoduleSettings> _settings =
            new();

        private IDreamfireProjectPathService _projectPathService;
        private IDreamfireLibraryDiscoveryService _discoveryService;
        private IDreamfireGitService _gitService;
        private IDreamfireGitLabService _gitLabService;
        private IDreamfirePackageService _packageService;
        private IDreamfireLibraryUpdateService _updateService;
        private IDreamfirePermissionAuthorizationService
            _permissionAuthorizationService;

        private DreamfireAuthenticationSession
            _authenticationSession;
        private DreamfireInMemoryGitLabAccessTokenProvider
            _tokenProvider;
        private DreamfireGitLabApiClient _gitLabApiClient;
        private DreamfireAuthenticationCoordinator
            _authenticationCoordinator;
        private DreamfireGitLabAuthenticationPanel
            _authenticationPanel;
        private string _authenticationInitialisationError =
            string.Empty;
        private Uri _gitLabBaseUri;

        private DreamfireProjectContext _projectContext;

        private CancellationTokenSource _cancellationTokenSource;

        private Vector2 _scrollPosition;
        private string _searchText = string.Empty;
        private string _errorMessage = string.Empty;

        private DreamfireLibraryFilter _selectedFilter =
            DreamfireLibraryFilter.All;

        private bool _isDiscovering;
        private bool _isCheckingUpdates;

        private GUIStyle _statusBadgeStyle;
        private GUIStyle _cardHeaderStyle;

        [MenuItem(
            "Dreamfire Submodules/Dreamfire Library Manager")]
        public static void Open()
        {
            DreamfireLibraryManagerWindow window =
                GetWindow<DreamfireLibraryManagerWindow>();

            window.titleContent =
                new GUIContent("Dreamfire Libraries");

            window.minSize =
                new Vector2(760f, 500f);

            window.Show();
        }

        private void OnEnable()
        {
            CreateAuthenticationServices();

            if (_authenticationSession == null)
            {
                return;
            }

            CreateServices();
            LoadSettings();

            EditorApplication.delayCall -=
                BeginInitialDiscovery;

            EditorApplication.delayCall +=
                BeginInitialDiscovery;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -=
                BeginInitialDiscovery;

            CancelCurrentOperation();
            DisposeAuthenticationServices();
        }

        private void CreateServices()
        {
            DreamfireVersionService versionService =
                new();

            _projectPathService =
                new DreamfireProjectPathService();

            _discoveryService =
                new DreamfireLibraryDiscoveryService(
                    versionService);

            DreamfireLibraryGitProcessRunner processRunner =
                new();

            _permissionAuthorizationService =
                new DreamfirePermissionAuthorizationService(
                    _authenticationSession);

            _gitService =
                new DreamfireGitService(
                    processRunner,
                    _permissionAuthorizationService,
                    _tokenProvider,
                    _gitLabBaseUri);

            _gitLabService =
                new DreamfireGitLabService(
                    _gitLabBaseUri,
                    _tokenProvider,
                    _permissionAuthorizationService);

            _packageService =
                new DreamfirePackageService();

            _updateService =
                new DreamfireLibraryUpdateService(
                    _gitService,
                    _gitLabService,
                    versionService,
                    _packageService,
                    _permissionAuthorizationService);
        }

        private void BeginInitialDiscovery()
        {
            if (this == null)
            {
                return;
            }

            if (!CanUsePermission(
                    DreamfirePermission.ViewLibraries,
                    "view Dreamfire libraries",
                    out _))
            {
                return;
            }

            _ = DiscoverLibrariesAsync();
        }

        private void OnGUI()
        {
            if (_authenticationPanel == null)
            {
                DrawAuthenticationConfiguration();
                return;
            }

            _authenticationPanel.Draw();

            EditorGUILayout.Space(8f);

            EnsureStyles();

            DrawHeader();

            if (!CanUsePermission(
                    DreamfirePermission.ViewLibraries,
                    "view Dreamfire libraries",
                    out string accessFailure))
            {
                EditorGUILayout.HelpBox(
                    accessFailure,
                    MessageType.Warning);

                return;
            }

            DrawProjectContext();
            DrawToolbar();
            DrawSearchAndFilter();
            DrawLibraryList();
        }

        private void EnsureStyles()
        {
            _statusBadgeStyle ??=
                new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };

            _cardHeaderStyle ??=
                new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontStyle = FontStyle.Bold
                };
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Dreamfire Library Manager",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Manage local Dreamfire libraries, Git submodules, " +
                "updates and Unity package requirements.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(8f);
        }

        private void DrawProjectContext()
        {
            if (!string.IsNullOrWhiteSpace(_errorMessage))
            {
                EditorGUILayout.HelpBox(
                    _errorMessage,
                    MessageType.Error);
            }

            if (_projectContext == null)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "Unity Project",
                _projectContext.UnityProjectRoot,
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                "Git Repository",
                _projectContext.IsInsideGitRepository
                    ? _projectContext.GitRepositoryRoot
                    : "Not detected",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(4f);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.toolbar))
            {
                GUILayout.Label(
                    $"Libraries: {_libraries.Count}",
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(
                           _isDiscovering))
                {
                    if (GUILayout.Button(
                            _isDiscovering
                                ? "Refreshing..."
                                : "Refresh",
                            EditorStyles.toolbarButton,
                            GUILayout.Width(90f)))
                    {
                        _ = DiscoverLibrariesAsync();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           _libraries.Count == 0 ||
                           _isDiscovering ||
                           _isCheckingUpdates))
                {
                    if (GUILayout.Button(
                            _isCheckingUpdates
                                ? "Checking..."
                                : "Check All Updates",
                            EditorStyles.toolbarButton,
                            GUILayout.Width(125f)))
                    {
                        _ = CheckAllUpdatesAsync();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           _projectContext == null ||
                           !_projectContext.IsInsideGitRepository ||
                           _settings.Count == 0 ||
                           !CanUsePermission(
                               DreamfirePermission
                                   .InstallLibraries,
                               "install a Dreamfire library",
                               out _)))
                {
                    if (GUILayout.Button(
                            "Install Library",
                            EditorStyles.toolbarButton,
                            GUILayout.Width(100f)))
                    {
                        OpenInstallLibraryWindow();
                    }
                }
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawSearchAndFilter()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _searchText =
                    EditorGUILayout.TextField(
                        _searchText,
                        EditorStyles.toolbarSearchField);

                _selectedFilter =
                    (DreamfireLibraryFilter)
                    EditorGUILayout.EnumPopup(
                        _selectedFilter,
                        EditorStyles.toolbarPopup,
                        GUILayout.Width(155f));

                if (GUILayout.Button(
                        "Clear",
                        GUILayout.Width(55f)))
                {
                    _searchText = string.Empty;

                    _selectedFilter =
                        DreamfireLibraryFilter.All;

                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawLibraryList()
        {
            if (_isDiscovering &&
                _libraries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Discovering local Dreamfire libraries...",
                    MessageType.Info);

                return;
            }

            List<DreamfireLibraryState> visibleLibraries =
                GetVisibleLibraries();

            if (visibleLibraries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    _libraries.Count == 0
                        ? "No installed Dreamfire libraries were found."
                        : "No libraries match the current search or filter.",
                    MessageType.Info);

                return;
            }

            _scrollPosition =
                EditorGUILayout.BeginScrollView(
                    _scrollPosition);

            foreach (DreamfireLibraryState library in
                     visibleLibraries)
            {
                DrawLibraryCard(library);
            }

            EditorGUILayout.EndScrollView();
        }

        private List<DreamfireLibraryState>
            GetVisibleLibraries()
        {
            IEnumerable<DreamfireLibraryState> query =
                _libraries;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string search =
                    _searchText.Trim();

                query = query.Where(
                    library =>
                        library.DisplayName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase) ||
                        library.Name.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase) ||
                        library.RelativePath.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase));
            }

            query = _selectedFilter switch
            {
                DreamfireLibraryFilter.Installed =>
                    query.Where(
                        library => library.IsInstalled),

                DreamfireLibraryFilter.Submodules =>
                    query.Where(
                        library => library.IsSubmodule),

                DreamfireLibraryFilter.LocalFolders =>
                    query.Where(
                        library => !library.IsSubmodule),

                DreamfireLibraryFilter.UpdatesAvailable =>
                    query.Where(
                        library => library.HasUpdateAvailable),

                DreamfireLibraryFilter.MissingPackages =>
                    query.Where(
                        library => library.HasMissingPackages),

                DreamfireLibraryFilter.Errors =>
                    query.Where(
                        library => library.HasError),

                _ => query
            };

            return query
                .OrderBy(
                    library => library.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void DrawLibraryCard(
            DreamfireLibraryState library)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                DrawLibraryHeader(library);

                if (!library.IsExpanded)
                {
                    return;
                }

                EditorGUILayout.Space(3f);

                DrawLibraryDetails(library);
                DrawLibraryPackages(library);
                DrawLibraryMessages(library);
                DrawLibraryActions(library);
            }

            EditorGUILayout.Space(3f);
        }

        private void DrawLibraryHeader(
            DreamfireLibraryState library)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                library.IsExpanded =
                    EditorGUILayout.Foldout(
                        library.IsExpanded,
                        library.DisplayName,
                        true,
                        _cardHeaderStyle);

                GUILayout.FlexibleSpace();

                GUILayout.Label(
                    GetLibraryBadge(library),
                    _statusBadgeStyle,
                    GUILayout.Width(130f));
            }
        }

        private static string GetLibraryBadge(
            DreamfireLibraryState library)
        {
            if (library.IsBusy)
            {
                return "Working...";
            }

            if (library.HasError)
            {
                return "Error";
            }

            if (library.HasUpdateAvailable)
            {
                return "Update Available";
            }

            if (library.HasMissingPackages)
            {
                return "Missing Packages";
            }

            return library.IsSubmodule
                ? "Submodule"
                : "Local Folder";
        }

        private static void DrawLibraryDetails(
            DreamfireLibraryState library)
        {
            DrawDetailRow(
                "Status",
                library.IsInstalled
                    ? "Installed"
                    : "Not Installed");

            DrawDetailRow(
                "Version",
                library.InstalledVersion);

            DrawDetailRow(
                "Latest",
                string.IsNullOrWhiteSpace(
                    library.LatestVersion)
                    ? "Not checked"
                    : library.LatestVersion);

            DrawDetailRow(
                "Branch",
                string.IsNullOrWhiteSpace(
                    library.CurrentBranch)
                    ? "Loading..."
                    : library.CurrentBranch);

            DrawDetailRow(
                "Git",
                GetGitStateText(library));

            DrawDetailRow(
                "Path",
                library.RelativePath);
        }

        private static string GetGitStateText(
            DreamfireLibraryState library)
        {
            if (library.HasLocalChanges)
            {
                return "Uncommitted changes";
            }

            if (library.HasUnpushedCommits)
            {
                return library.AheadCount == 1
                    ? "1 unpushed commit"
                    : $"{library.AheadCount} unpushed commits";
            }

            if (library.BehindCount > 0)
            {
                return library.BehindCount == 1
                    ? "1 commit behind"
                    : $"{library.BehindCount} commits behind";
            }

            return "Clean and pushed";
        }

        private static void DrawLibraryPackages(
            DreamfireLibraryState library)
        {
            if (library.Packages.Count == 0)
            {
                DrawDetailRow(
                    "Packages",
                    library.HasMissingPackages
                        ? "Requirements not checked"
                        : "OK");

                return;
            }

            EditorGUILayout.Space(2f);

            EditorGUILayout.LabelField(
                "Package Requirements",
                EditorStyles.miniBoldLabel);

            foreach (DreamfirePackageState package in
                     library.Packages)
            {
                string packageName =
                    package.Requirement?.packageName ??
                    "Invalid package";

                string requiredVersion =
                    package.Requirement?.version ??
                    string.Empty;

                string status =
                    package.Status switch
                    {
                        DreamfirePackageStatus.Installed =>
                            $"Installed {package.InstalledVersion}",

                        DreamfirePackageStatus.Missing =>
                            $"Missing — requires {requiredVersion}",

                        DreamfirePackageStatus.Outdated =>
                            $"Outdated — installed " +
                            $"{package.InstalledVersion}, " +
                            $"requires {requiredVersion}",

                        DreamfirePackageStatus.Invalid =>
                            "Invalid requirement",

                        _ => "Unknown"
                    };

                DrawDetailRow(
                    packageName,
                    status);
            }
        }

        private void DrawLibraryMessages(
            DreamfireLibraryState library)
        {
            if (library.IsBusy)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrWhiteSpace(
                        library.BusyMessage)
                        ? "Operation in progress..."
                        : library.BusyMessage,
                    MessageType.Info);
            }

            if (library.HasError)
            {
                EditorGUILayout.HelpBox(
                    library.ErrorMessage,
                    MessageType.Error);
            }

            if (library.IsSubmodule &&
                !library.IsSubmoduleInitialized)
            {
                EditorGUILayout.HelpBox(
                    "This submodule is configured but not initialized.",
                    MessageType.Warning);
            }

            if (library.HasLocalChanges)
            {
                EditorGUILayout.HelpBox(
                    "This library contains uncommitted changes. " +
                    "Commit or discard them before pulling or updating.",
                    MessageType.Warning);

                library.CommitMessage =
                    EditorGUILayout.TextField(
                        "Commit Message",
                        library.CommitMessage);
            }
            else if (library.HasUnpushedCommits)
            {
                EditorGUILayout.HelpBox(
                    library.AheadCount == 1
                        ? "This library has 1 commit waiting to be pushed."
                        : $"This library has {library.AheadCount} " +
                          "commits waiting to be pushed.",
                    MessageType.Info);
            }

            if (!string.IsNullOrWhiteSpace(
                    library.StatusMessage))
            {
                EditorGUILayout.HelpBox(
                    library.StatusMessage,
                    MessageType.None);
            }
        }

        private void DrawLibraryActions(
            DreamfireLibraryState library)
        {
            EditorGUILayout.Space(3f);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool canCheck =
                    !library.IsBusy &&
                    library.IsInstalled;

                using (new EditorGUI.DisabledScope(!canCheck))
                {
                    if (GUILayout.Button(
                            "Check",
                            GUILayout.Width(70f)))
                    {
                        _ = CheckLibraryUpdateAsync(
                            library);
                    }
                }

                bool canUpdate =
                    !library.IsBusy &&
                    library.HasUpdateAvailable &&
                    !library.HasLocalChanges &&
                    CanUsePermission(
                        DreamfirePermission.UpdateLibraries,
                        "update a Dreamfire library",
                        out _);

                using (new EditorGUI.DisabledScope(!canUpdate))
                {
                    if (GUILayout.Button(
                            "Update",
                            GUILayout.Width(75f)))
                    {
                        _ = UpdateLibraryAsync(
                            library);
                    }
                }

                bool canCommit =
                    !library.IsBusy &&
                    library.HasLocalChanges &&
                    !string.IsNullOrWhiteSpace(
                        library.CommitMessage) &&
                    CanUsePermission(
                        DreamfirePermission
                            .ExecuteLocalHistoryGitCommands,
                        "commit Dreamfire library changes",
                        out _);

                using (new EditorGUI.DisabledScope(!canCommit))
                {
                    if (GUILayout.Button(
                            "Commit",
                            GUILayout.Width(75f)))
                    {
                        _ = CommitLibraryAsync(
                            library);
                    }
                }

                bool canPull =
                    !library.IsBusy &&
                    library.IsSubmoduleInitialized &&
                    !library.HasLocalChanges &&
                    CanUsePermission(
                        DreamfirePermission.UpdateLibraries,
                        "update a Dreamfire library",
                        out _);

                using (new EditorGUI.DisabledScope(!canPull))
                {
                    if (GUILayout.Button(
                            "Pull",
                            GUILayout.Width(65f)))
                    {
                        _ = PullLibraryAsync(
                            library);
                    }
                }

                bool canPush =
                    !library.IsBusy &&
                    library.IsSubmoduleInitialized &&
                    !library.HasLocalChanges &&
                    library.HasUnpushedCommits &&
                    CanUsePermission(
                        DreamfirePermission
                            .ExecuteRemoteGitCommands,
                        "push Dreamfire library changes",
                        out _);

                using (new EditorGUI.DisabledScope(!canPush))
                {
                    if (GUILayout.Button(
                            "Push",
                            GUILayout.Width(65f)))
                    {
                        _ = PushLibraryAsync(
                            library);
                    }
                }

                bool canInstallPackages =
                    !library.IsBusy &&
                    library.HasMissingPackages &&
                    CanUsePermission(
                        DreamfirePermission.InstallLibraries,
                        "install Dreamfire library packages",
                        out _);

                using (new EditorGUI.DisabledScope(
                           !canInstallPackages))
                {
                    if (GUILayout.Button(
                            "Install Packages",
                            GUILayout.Width(115f)))
                    {
                        _ = InstallMissingPackagesAsync(
                            library);
                    }
                }

                bool canRemove =
                    !library.IsBusy &&
                    library.IsSubmodule &&
                    !library.HasLocalChanges &&
                    CanUsePermission(
                        DreamfirePermission.RemoveLibraries,
                        "remove a Dreamfire library",
                        out _);

                using (new EditorGUI.DisabledScope(!canRemove))
                {
                    if (GUILayout.Button(
                            "Remove",
                            GUILayout.Width(75f)))
                    {
                        ConfirmRemoveLibrary(library);
                    }
                }

                if (GUILayout.Button(
                        "Open Folder",
                        GUILayout.Width(95f)))
                {
                    EditorUtility.RevealInFinder(
                        library.AbsolutePath);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private static void DrawDetailRow(
            string label,
            string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    label,
                    GUILayout.Width(95f));

                EditorGUILayout.SelectableLabel(
                    value ?? string.Empty,
                    EditorStyles.miniLabel,
                    GUILayout.Height(
                        EditorGUIUtility.singleLineHeight));
            }
        }

        private async Task DiscoverLibrariesAsync()
        {
            if (_isDiscovering)
            {
                return;
            }

            LoadSettings();

            if (_settings.Count == 0)
            {
                _libraries.Clear();

                _errorMessage =
                    "No DreamfireSubmoduleSettings assets were found.";

                Repaint();
                return;
            }

            _isDiscovering = true;
            _errorMessage = string.Empty;

            ResetCancellationToken();

            CancellationToken token =
                _cancellationTokenSource.Token;

            Repaint();

            try
            {
                ServiceResult<DreamfireProjectContext>
                    contextResult =
                        _projectPathService
                            .DetectProjectContext();

                if (!contextResult.IsSuccess)
                {
                    _errorMessage =
                        contextResult.Error;

                    return;
                }

                _projectContext =
                    contextResult.Value;

                DreamfireSubmoduleSettings discoverySettings =
                    _settings[0];

                ServiceResult<
                        IReadOnlyList<DreamfireLibraryState>>
                    discoveryResult =
                        await _discoveryService
                            .DiscoverLibrariesAsync(
                                _projectContext.UnityProjectRoot,
                                discoverySettings,
                                token);

                if (!discoveryResult.IsSuccess)
                {
                    _errorMessage =
                        discoveryResult.Error;

                    return;
                }

                _libraries.Clear();
                _libraries.AddRange(
                    discoveryResult.Value);

                Repaint();

                await LoadLocalGitStatesAsync(token);
                await LoadLocalPackageStatesAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Operation cancelled.
            }
            catch (Exception exception)
            {
                _errorMessage =
                    exception.Message;

                Debug.LogException(exception);
            }
            finally
            {
                _isDiscovering = false;

                if (this != null)
                {
                    Repaint();
                }
            }
        }

        private async Task LoadLocalGitStatesAsync(
            CancellationToken cancellationToken)
        {
            foreach (DreamfireLibraryState library in
                     _libraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!library.IsSubmoduleInitialized)
                {
                    continue;
                }

                DreamfireGitStatus status =
                    await _gitService.GetStatusAsync(
                        library.AbsolutePath,
                        cancellationToken);

                ApplyGitStatus(
                    library,
                    status);

                Repaint();
            }
        }

        private async Task LoadLocalPackageStatesAsync(
            CancellationToken cancellationToken)
        {
            foreach (DreamfireLibraryState library in
                     _libraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(
                        library.PackageManifestPath))
                {
                    continue;
                }

                await RefreshPackageStateAsync(
                    library,
                    cancellationToken);

                Repaint();
            }
        }

        private async Task RefreshPackageStateAsync(
            DreamfireLibraryState library,
            CancellationToken cancellationToken)
        {
            ServiceResult<
                    Scripts.DreamfireManifest
                        .DreamfirePackageManifest>
                manifestResult =
                    await _packageService.ReadManifestAsync(
                        library.PackageManifestPath,
                        cancellationToken);

            if (!manifestResult.IsSuccess)
            {
                library.ErrorMessage =
                    manifestResult.Error;

                return;
            }

            library.PackageManifest =
                manifestResult.Value;

            ServiceResult<
                    IReadOnlyList<DreamfirePackageState>>
                packageResult =
                    await _packageService
                        .CheckRequirementsAsync(
                            manifestResult.Value,
                            cancellationToken);

            if (!packageResult.IsSuccess)
            {
                library.ErrorMessage =
                    packageResult.Error;

                return;
            }

            library.Packages.Clear();
            library.Packages.AddRange(
                packageResult.Value);

            library.HasMissingPackages =
                packageResult.Value.Any(
                    package =>
                        package.Status ==
                        DreamfirePackageStatus.Missing ||
                        package.Status ==
                        DreamfirePackageStatus.Outdated);
        }

        private async Task CheckAllUpdatesAsync()
        {
            if (_isCheckingUpdates)
            {
                return;
            }

            _isCheckingUpdates = true;

            ResetCancellationToken();

            try
            {
                foreach (DreamfireLibraryState library in
                         _libraries)
                {
                    await CheckLibraryUpdateAsync(
                        library,
                        false);
                }
            }
            finally
            {
                _isCheckingUpdates = false;
                Repaint();
            }
        }

        private Task CheckLibraryUpdateAsync(
            DreamfireLibraryState library)
        {
            return CheckLibraryUpdateAsync(
                library,
                true);
        }

        private async Task CheckLibraryUpdateAsync(
            DreamfireLibraryState library,
            bool repaint)
        {
            DreamfireSubmoduleSettings settings =
                FindSettingsForLibrary(library);

            if (settings == null)
            {
                library.ErrorMessage =
                    "No matching GitLab settings were found.";

                return;
            }

            library.IsBusy = true;
            library.BusyMessage =
                "Checking latest version...";

            library.ErrorMessage = string.Empty;

            try
            {
                ServiceResult result =
                    await _updateService
                        .CheckForUpdateAsync(
                            library,
                            settings,
                            _cancellationTokenSource?.Token ??
                            CancellationToken.None);

                if (!result.IsSuccess)
                {
                    library.ErrorMessage =
                        result.Error;

                    return;
                }

                library.StatusMessage =
                    library.HasUpdateAvailable
                        ? $"Version {library.LatestVersion} is available."
                        : "Library is up to date.";
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;

                if (repaint)
                {
                    Repaint();
                }
            }
        }

        private async Task UpdateLibraryAsync(
            DreamfireLibraryState library)
        {
            DreamfireSubmoduleSettings settings =
                FindSettingsForLibrary(library);

            if (settings == null)
            {
                library.ErrorMessage =
                    "No matching GitLab settings were found.";

                return;
            }

            library.IsBusy = true;
            library.BusyMessage =
                $"Updating {library.DisplayName}...";

            library.ErrorMessage = string.Empty;

            try
            {
                ServiceResult result =
                    await _updateService.UpdateAsync(
                        library,
                        settings,
                        true,
                        _cancellationTokenSource?.Token ??
                        CancellationToken.None);

                if (!result.IsSuccess)
                {
                    library.ErrorMessage =
                        result.Error;

                    return;
                }

                library.StatusMessage =
                    "Library updated successfully.";

                await RefreshLibraryGitStateAsync(
                    library);

                await RefreshPackageStateAsync(
                    library,
                    CancellationToken.None);

                await DiscoverLibrariesAsync();
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;
                Repaint();
            }
        }

        private async Task CommitLibraryAsync(
            DreamfireLibraryState library)
        {
            library.IsBusy = true;
            library.BusyMessage =
                $"Committing {library.DisplayName}...";

            library.ErrorMessage = string.Empty;

            try
            {
                DreamfireGitResult result =
                    await _gitService.CommitAllAsync(
                        library.AbsolutePath,
                        library.CommitMessage,
                        CancellationToken.None);

                if (!result.Success)
                {
                    library.ErrorMessage =
                        BuildGitError(
                            "Git commit failed.",
                            result);

                    return;
                }

                library.CommitMessage =
                    string.Empty;

                library.StatusMessage =
                    "Changes committed successfully.";

                await RefreshLibraryGitStateAsync(
                    library);
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;
                Repaint();
            }
        }

        private async Task PullLibraryAsync(
            DreamfireLibraryState library)
        {
            library.IsBusy = true;
            library.BusyMessage =
                $"Pulling {library.DisplayName}...";

            library.ErrorMessage = string.Empty;

            try
            {
                DreamfireGitResult result =
                    await _gitService.PullAsync(
                        library.AbsolutePath,
                        library.CurrentBranch,
                        CancellationToken.None);

                if (!result.Success)
                {
                    library.ErrorMessage =
                        BuildGitError(
                            "Git pull failed.",
                            result);

                    return;
                }

                AssetDatabase.Refresh();

                library.StatusMessage =
                    "Library pulled successfully.";

                await RefreshLibraryGitStateAsync(
                    library);

                await RefreshPackageStateAsync(
                    library,
                    CancellationToken.None);
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;
                Repaint();
            }
        }

        private async Task PushLibraryAsync(
            DreamfireLibraryState library)
        {
            library.IsBusy = true;
            library.BusyMessage =
                $"Pushing {library.DisplayName}...";

            library.ErrorMessage = string.Empty;

            try
            {
                DreamfireGitResult result =
                    await _gitService.PushAsync(
                        library.AbsolutePath,
                        library.CurrentBranch,
                        CancellationToken.None);

                if (!result.Success)
                {
                    library.ErrorMessage =
                        BuildGitError(
                            "Git push failed.",
                            result);

                    return;
                }

                library.StatusMessage =
                    "Library pushed successfully.";

                await RefreshLibraryGitStateAsync(
                    library);
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;
                Repaint();
            }
        }

        private async Task InstallMissingPackagesAsync(
            DreamfireLibraryState library)
        {
            library.IsBusy = true;
            library.BusyMessage =
                "Installing missing Unity packages...";

            library.ErrorMessage = string.Empty;

            try
            {
                ServiceResult result =
                    await _packageService.InstallMissingAsync(
                        library.Packages,
                        CancellationToken.None);

                if (!result.IsSuccess)
                {
                    library.ErrorMessage =
                        result.Error;

                    return;
                }

                await RefreshPackageStateAsync(
                    library,
                    CancellationToken.None);

                library.StatusMessage =
                    "Required Unity packages were installed.";
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;
                Repaint();
            }
        }

        private async Task RefreshLibraryGitStateAsync(
            DreamfireLibraryState library)
        {
            DreamfireGitStatus status =
                await _gitService.GetStatusAsync(
                    library.AbsolutePath,
                    CancellationToken.None);

            ApplyGitStatus(
                library,
                status);
        }

        private static void ApplyGitStatus(
            DreamfireLibraryState library,
            DreamfireGitStatus status)
        {
            library.CurrentBranch =
                string.IsNullOrWhiteSpace(status.Branch)
                    ? "Detached HEAD"
                    : status.Branch;

            library.HasLocalChanges =
                status.IsDirty;

            library.HasUnpushedCommits =
                status.HasUnpushedCommits;

            library.AheadCount =
                status.AheadCount;

            library.BehindCount =
                status.BehindCount;
        }

        private void OpenInstallLibraryWindow()
        {
            DreamfireInstallLibraryWindow.Open(
                _settings,
                _projectContext.UnityProjectRoot,
                _projectContext.GitRepositoryRoot,
                _gitLabService,
                _gitService,
                _permissionAuthorizationService);
        }

        private void ConfirmRemoveLibrary(
            DreamfireLibraryState library)
        {
            if (library == null)
            {
                return;
            }

            bool confirmed =
                EditorUtility.DisplayDialog(
                    "Remove Dreamfire Library",
                    $"Remove '{library.DisplayName}' from this " +
                    "project? This deinitialises the Git submodule " +
                    "and stages its removal.",
                    "Remove Library",
                    "Cancel");

            if (confirmed)
            {
                _ = RemoveLibraryAsync(library);
            }
        }

        private async Task RemoveLibraryAsync(
            DreamfireLibraryState library)
        {
            if (!CanUsePermission(
                    DreamfirePermission.RemoveLibraries,
                    "remove a Dreamfire library",
                    out string permissionFailure))
            {
                library.ErrorMessage = permissionFailure;
                Repaint();
                return;
            }

            if (_projectContext == null ||
                string.IsNullOrWhiteSpace(
                    _projectContext.GitRepositoryRoot))
            {
                library.ErrorMessage =
                    "The parent Git repository could not be found.";

                Repaint();
                return;
            }

            string parentRepositoryPath =
                Path.GetFullPath(
                    _projectContext.GitRepositoryRoot);

            string relativeSubmodulePath =
                Path.GetRelativePath(
                        parentRepositoryPath,
                        Path.GetFullPath(
                            library.AbsolutePath))
                    .Replace('\\', '/');

            library.IsBusy = true;
            library.BusyMessage =
                $"Removing {library.DisplayName}...";
            library.ErrorMessage = string.Empty;

            try
            {
                DreamfireGitResult result =
                    await _gitService.RemoveSubmoduleAsync(
                        parentRepositoryPath,
                        relativeSubmodulePath,
                        CancellationToken.None);

                if (!result.Success)
                {
                    library.ErrorMessage =
                        BuildGitError(
                            "Library removal failed.",
                            result);

                    return;
                }

                AssetDatabase.Refresh();

                await DiscoverLibrariesAsync();
            }
            finally
            {
                library.IsBusy = false;
                library.BusyMessage = string.Empty;
                Repaint();
            }
        }

        private DreamfireSubmoduleSettings
            FindSettingsForLibrary(
                DreamfireLibraryState library)
        {
            if (_settings.Count == 0)
            {
                return null;
            }

            DreamfireSubmoduleSettings matching =
                _settings.FirstOrDefault(
                    settings =>
                        library.Name.StartsWith(
                            "Autodidactic",
                            StringComparison.OrdinalIgnoreCase)
                            ? settings.GitLabGroupPath.Contains(
                                "autodidactic",
                                StringComparison.OrdinalIgnoreCase)
                            : settings.GitLabGroupPath.Contains(
                                "dreamfire-studios",
                                StringComparison.OrdinalIgnoreCase));

            return matching ?? _settings[0];
        }

        private void LoadSettings()
        {
            _settings.Clear();

            string[] guids =
                AssetDatabase.FindAssets(
                    SettingsFilter);

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guid);

                DreamfireSubmoduleSettings settings =
                    AssetDatabase.LoadAssetAtPath<
                        DreamfireSubmoduleSettings>(
                        path);

                if (settings != null)
                {
                    _settings.Add(settings);
                }
            }
        }

        private void ResetCancellationToken()
        {
            CancelCurrentOperation();

            _cancellationTokenSource =
                new CancellationTokenSource();
        }

        private void CancelCurrentOperation()
        {
            if (_cancellationTokenSource == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private static string BuildGitError(
            string message,
            DreamfireGitResult result)
        {
            string details =
                !string.IsNullOrWhiteSpace(result.Error)
                    ? result.Error.Trim()
                    : result.Output.Trim();

            return string.IsNullOrWhiteSpace(details)
                ? message
                : $"{message}\n{details}";
        }

        private void CreateAuthenticationServices()
        {
            if (_authenticationPanel != null)
            {
                return;
            }

            _authenticationInitialisationError =
                string.Empty;

            DreamfireGitLabProjectSettings settings =
                DreamfireGitLabProjectSettings.instance;

            if (!settings.TryGetConfiguration(
                    out Uri gitLabBaseUri,
                    out long authorityProjectId,
                    out string validationError))
            {
                _authenticationInitialisationError =
                    validationError;

                return;
            }

            _gitLabBaseUri = gitLabBaseUri;

            _authenticationSession =
                new DreamfireAuthenticationSession();

            _authenticationSession.Changed +=
                HandleAuthenticationChanged;

            _tokenProvider =
                new DreamfireInMemoryGitLabAccessTokenProvider();

            _gitLabApiClient =
                new DreamfireGitLabApiClient(
                    gitLabBaseUri,
                    authorityProjectId);

            _authenticationCoordinator =
                new DreamfireAuthenticationCoordinator(
                    _gitLabApiClient,
                    _tokenProvider,
                    _authenticationSession);

            _authenticationPanel =
                new DreamfireGitLabAuthenticationPanel(
                    _authenticationCoordinator,
                    _authenticationSession,
                    Repaint);
        }

        private void DisposeAuthenticationServices()
        {
            EditorApplication.delayCall -=
                BeginInitialDiscovery;

            _authenticationPanel?.Dispose();
            _authenticationPanel = null;

            if (_authenticationSession != null)
            {
                _authenticationSession.Changed -=
                    HandleAuthenticationChanged;
            }

            _authenticationCoordinator?.Dispose();
            _authenticationCoordinator = null;

            _gitLabApiClient?.Dispose();
            _gitLabApiClient = null;

            _tokenProvider?.Dispose();
            _tokenProvider = null;

            _authenticationSession = null;
            _permissionAuthorizationService = null;
            _gitLabBaseUri = null;

            _gitService = null;
            _gitLabService = null;
            _updateService = null;
        }

        private void HandleAuthenticationChanged()
        {
            if (CanUsePermission(
                    DreamfirePermission.ViewLibraries,
                    "view Dreamfire libraries",
                    out _))
            {
                EditorApplication.delayCall -=
                    BeginInitialDiscovery;

                EditorApplication.delayCall +=
                    BeginInitialDiscovery;
            }
            else
            {
                CancelCurrentOperation();
                _libraries.Clear();
                _projectContext = null;
            }

            Repaint();
        }

        private bool CanUsePermission(
            DreamfirePermission requiredPermission,
            string operationName,
            out string failureReason)
        {
            if (_permissionAuthorizationService == null)
            {
                failureReason =
                    "Authentication services are unavailable.";

                return false;
            }

            global::DreamfireSubmodules.Scripts.ServiceResult.ServiceResult
                authorizationResult =
                    _permissionAuthorizationService.Authorize(
                        requiredPermission,
                        operationName);

            failureReason =
                authorizationResult.IsFailure
                    ? authorizationResult.Error
                    : string.Empty;

            return !authorizationResult.IsFailure;
        }

        private void DrawAuthenticationConfiguration()
        {
            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "GitLab Account",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(
                    _authenticationInitialisationError)
                    ? "Configure GitLab authentication in Project Settings."
                    : _authenticationInitialisationError,
                MessageType.Warning);

            if (GUILayout.Button(
                    "Open GitLab Project Settings",
                    GUILayout.Height(28f)))
            {
                SettingsService.OpenProjectSettings(
                    DreamfireGitLabProjectSettings.SettingsPath);
            }

            if (GUILayout.Button(
                    "Retry Authentication Setup",
                    GUILayout.Height(24f)))
            {
                RetryAuthenticationSetup();
            }
        }

        private void RetryAuthenticationSetup()
        {
            DisposeAuthenticationServices();
            CreateAuthenticationServices();

            if (_authenticationSession == null)
            {
                Repaint();
                return;
            }

            CreateServices();
            LoadSettings();
            Repaint();
        }
    }
}

#endif