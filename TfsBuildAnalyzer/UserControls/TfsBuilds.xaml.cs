using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Types;
using TfsBuildAnalyzer.Utilities;
using TfsBuildAnalyzer.Windows;


namespace TfsBuildAnalyzer.UserControls
{
    /// <summary>
    /// Interaction logic for TfsBuilds.xaml
    /// </summary>
    public partial class TfsBuilds
    {
        private BuildType myShowingCompletedBuildsForBuildType;
        private List<CompletedBuild> myCompletedBuilds;

        private CancellationTokenSource myGetCompletedBuildsCancellationTokenSource;
        private string myTfsUserName;

        private Dispatcher myDispatcherObject;

        public TfsBuilds()
        {
            InitializeComponent();
        }

        private void TfsBuilds_OnInitialized(object sender, EventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            myDispatcherObject = Application.Current.Dispatcher;
            SettingsWindow.ProjectStructureCleared += OnProjectStructureCleared;
            UpdateCollectionsAndProjects();
        }

        private void OnProjectStructureCleared(object sender, EventArgs e)
        {
            ProjectCatalogComboBox.ItemsSource = null;
            ProjectCatalogComboBox.DataContext = null;

            ProjectTypeComboBox.ItemsSource = null;
            ProjectTypeComboBox.DataContext = null;

            FavoriteBuildNamesGrid.ItemsSource = null;
            FavoriteBuildNamesGrid.DataContext = null;

            AllBuildNamesGrid.ItemsSource = null;
            AllBuildNamesGrid.DataContext = null;

            BuildDetailsGrid.ItemsSource = null;
            BuildDetailsGrid.DataContext = null;

            UpdateCollectionsAndProjects();
        }

        private void UpdateCollectionsAndProjects()
        {
            LoadingBuildsInfoPanel.Visibility = Visibility.Visible;
            LoadingBuildsInfoLabel.Content = "Loading TFS projects ...";

            GetCollectionsAndProjects(isSuccessful =>
            {
                RunOnUIThread(() =>
                {
                    if (isSuccessful)
                    {
                        LoadingBuildsInfoPanel.Visibility = Visibility.Collapsed;

                        ProjectCatalogComboBox.ItemsSource = TfsBuildAnalyzerConfiguration.ProjectStructure.ProjectCatalogNodeList.Select(x => new ComboBoxKeyValue(x.Id.ToString(), x.Name));
                        if (ProjectCatalogComboBox.Items.Count == 0) return;

                        SelectProjectCatalogInComboBox();
                    }
                    else
                    {
                        LoadingBuildsInfoLabel.Content = "An error occurred while loading TFS projects. Please retry.";
                        ReloadBuildDefinitionsButton.Visibility = Visibility.Visible;
                    }
                });
            });
        }

        private static async void GetCollectionsAndProjects(Action<bool> onCompleted)
        {
            var isSuccessful = false;
            var projectCatalogNodes = new List<ProjectCatalogNode>();
            var task = Task.Run(() =>
            {
                var projectStructure = TfsBuildAnalyzerConfiguration.ProjectStructure;
                if (projectStructure != null && projectStructure.ProjectCatalogNodeList.Count > 0)
                {
                    projectCatalogNodes.AddRange(projectStructure.ProjectCatalogNodeList);
                    isSuccessful = true;
                    return;
                }

                try
                {
                    var interestedProjectCollections = UtilityMethods.CommaSeparatedStringToList(TfsBuildAnalyzerConfiguration.IncludedProjectCollections);
                    var interestedProjectTypes = UtilityMethods.CommaSeparatedStringToList(TfsBuildAnalyzerConfiguration.IncludedProjectTypes);

                    using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(TfsBuildAnalyzerConfiguration.TfsServerPath)))
                    {
                        var catalogNodes = tfsConfigurationServer.CatalogNode
                            .QueryChildren(new[] { CatalogResourceTypes.ProjectCollection }, false, CatalogQueryOptions.None).Where(x =>
                                interestedProjectCollections.Count == 0 || interestedProjectCollections.CaseInsensitiveContains(x.Resource.DisplayName))
                            .ToList();

                        Parallel.ForEach(catalogNodes, catalogNode =>
                        {
                            var collectionId = new Guid(catalogNode.Resource.Properties["InstanceId"]);
                            // ReSharper disable once AccessToDisposedClosure
                            var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(collectionId);
                            var vcs = teamProjectCollection.GetService<VersionControlServer>();

                            var teamProjects = vcs.GetAllTeamProjects(true)
                                .Where(x => interestedProjectTypes.Count == 0 || interestedProjectTypes.CaseInsensitiveContains(x.Name)).ToList();

                            if (teamProjects.Count == 0)
                            {
                                return;
                            }

                            var node = new ProjectCatalogNode(catalogNode);
                            node.ProjectInfoNodes = teamProjects.Select(x => new ProjectInfoNode(Guid.NewGuid(), node.Id, x)).ToList();

                            projectCatalogNodes.Add(node);
                        });
                    }

                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    isSuccessful = false;
                    LogHelper.LogException(ex);
                }
            });

            await Task.Run(() =>
            {
                if (!task.Wait(TimeSpan.FromSeconds(30)))
                {
                    isSuccessful = false;
                }
            });
            if (isSuccessful)
            {
                var projectStructure = new ProjectStructure();
                projectStructure.ProjectCatalogNodeList.AddRange(projectCatalogNodes);
                TfsBuildAnalyzerConfiguration.ProjectStructure = projectStructure;
            }

            onCompleted(isSuccessful);
        }



        private async void UpdateAllBuildTypes(bool useCache)
        {
            var selectedProjectTypeNode = GetSelectedProjectTypeNode();
            if (selectedProjectTypeNode == null) return;

            FavoriteBuildNamesGrid.ItemsSource = new List<BuildType>();
            FavoriteBuildNamesGrid.DataContext = new List<BuildType>();

            AllBuildNamesGrid.ItemsSource = new List<BuildType>();
            AllBuildNamesGrid.DataContext = new List<BuildType>();

            BuildDetailsGrid.ItemsSource = new List<CompletedBuild>();
            BuildDetailsGrid.DataContext = new List<CompletedBuild>();

            LoadingBuildsInfoPanel.Visibility = Visibility.Visible;
            LoadingBuildsInfoLabel.Content = $"Loading all TFS builds for {selectedProjectTypeNode.Name} ...";

            ProjectCatalogComboBox.IsEnabled = false;
            ProjectTypeComboBox.IsEnabled = false;

            var gotResults = useCache && selectedProjectTypeNode.BuildTypes.Count > 0;

            if (!gotResults)
            {
                gotResults = await GetAllBuildTypes(selectedProjectTypeNode);
            }
            UpdateBuildDefinitionGrids();

            ProjectCatalogComboBox.IsEnabled = true;
            ProjectTypeComboBox.IsEnabled = true;

            if (gotResults)
            {
                LoadingBuildsInfoPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoadingBuildsInfoLabel.Content = $"An error occurred while loading TFS builds for {selectedProjectTypeNode.Name}. Please retry.";
                ReloadBuildDefinitionsButton.Visibility = Visibility.Visible;
            }
        }

        private async Task<bool> GetAllBuildTypes(ProjectInfoNode projectInfoNode)
        {
            var isSuccessful = false;
            var getAllBuildTypesTask  = Task.Run(() =>
            {
                var buildTypes = new List<BuildType>();
                try
                {
                    using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(TfsBuildAnalyzerConfiguration.TfsServerPath)))
                    {
                        var catalogNode = TfsBuildAnalyzerConfiguration.ProjectStructure.ProjectCatalogNodeList.First(x => x.Id.Equals(projectInfoNode.CatalogId));
                        var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(catalogNode.Id);

                        var bhc = teamProjectCollection.GetClient<BuildHttpClient>();
                        var buildDefinitions = bhc.GetDefinitionsAsync(projectInfoNode.Name).Result;

                        buildTypes.AddRange(buildDefinitions.Select(x =>
                            new BuildType
                            {
                                CollectionId = catalogNode.Id, TeamProjectName = projectInfoNode.Name, BuildName = x.Name,
                                Url = $"{TfsBuildAnalyzerConfiguration.TfsServerPath}/{teamProjectCollection.CatalogNode.Resource.DisplayName}/{projectInfoNode.Name}/_build?definitionId={x.Id}&_a=summary",
                                CatalogName = projectInfoNode.GetCatalogNode(TfsBuildAnalyzerConfiguration.ProjectStructure).Name,
                                VNextBuildDefinition = x, VNextBuildDefinitionId = x.Id, VNextBuildDefinitionProjectId = x.Project.Id
                            }));
                    }

                    buildTypes = buildTypes.OrderBy(x => x.BuildName).ToList();
                    projectInfoNode.BuildTypes = buildTypes;
                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    isSuccessful = false;
                    LogHelper.LogException(ex);
                }
            });

            await Task.Run(() =>
            {
                if (!getAllBuildTypesTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    isSuccessful = false;
                }
            });
            if (isSuccessful)
            {
                TfsBuildAnalyzerConfiguration.ProjectStructure.Save();
            }

            return isSuccessful;
        }

        private void UpdateBuildDefinitionGrids()
        {
            var selectedProjectTypeNode = GetSelectedProjectTypeNode();
            if (selectedProjectTypeNode == null) return;

            var favoriteBuildNames = UtilityMethods.CommaSeparatedStringToList(TfsBuildAnalyzerConfiguration.MyFavoriteBuilds);
            foreach (var buildType in selectedProjectTypeNode.BuildTypes)
            {
                buildType.IsFavorite = favoriteBuildNames.CaseInsensitiveContains(buildType.BuildName);
            }

            var searchText = AllBuildTypeSearchTextBox.Text.Trim();

            var parts = searchText.Split('*').Select(Regex.Escape).ToArray();
            var regex = string.Join(".*?", parts);

            var allBuildTypes = selectedProjectTypeNode.BuildTypes
                .Where(x => !x.IsFavorite && (string.IsNullOrEmpty(searchText) || Regex.IsMatch(x.BuildName, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline))).ToList();

            AllBuildNamesGrid.ItemsSource = allBuildTypes;
            AllBuildNamesGrid.DataContext = allBuildTypes;

            var favoriteBuildTypes = selectedProjectTypeNode.BuildTypes.Where(x => x.IsFavorite).ToList();
            FavoriteBuildNamesGrid.ItemsSource = favoriteBuildTypes;
            FavoriteBuildNamesGrid.DataContext = favoriteBuildTypes;
        }


        private async void GetCompletedBuilds(BuildType buildType)
        {
            ClearBuildDetailsGrid();
            CompletedBuildsStatusLabel.Content = $"Loading builds for {buildType.BuildName}...";
            myGetCompletedBuildsCancellationTokenSource?.Cancel();

            var currentCancellationTokenSource = new CancellationTokenSource(10000);
            try
            {

                myGetCompletedBuildsCancellationTokenSource = currentCancellationTokenSource;
                var gotBuilds = await GetCompletedBuildsAsync(buildType, myGetCompletedBuildsCancellationTokenSource.Token);
                if (gotBuilds && currentCancellationTokenSource.Equals(myGetCompletedBuildsCancellationTokenSource))
                {
                    UpdateCompletedBuildsGrid();
                    myShowingCompletedBuildsForBuildType = buildType;
                }
            }
            catch
            {
                if (currentCancellationTokenSource.Equals(myGetCompletedBuildsCancellationTokenSource))
                {
                    CompletedBuildsStatusLabel.Content = "Error occurred while loading builds.";
                }
            }
        }

        private async Task<bool> GetCompletedBuildsAsync(BuildType buildType, CancellationToken cancellationToken)
        {
            var isSuccessful= false;
            await Task.Run(() =>
            {
                try
                {
                    using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(TfsBuildAnalyzerConfiguration.TfsServerPath)))
                    {
                        if (string.IsNullOrEmpty(myTfsUserName))
                        {
                            myTfsUserName = tfsConfigurationServer.AuthorizedIdentity.DisplayName;
                        }

                        var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(buildType.CollectionId);
                        var bhc = teamProjectCollection.GetClient<BuildHttpClient>();
                        var vNextBuilds = bhc.GetBuildsAsync(definitions: new List<int> { buildType.VNextBuildDefinitionId },
                            project: buildType.VNextBuildDefinitionProjectId, cancellationToken: cancellationToken).Result;

                        var completedVNextBuilds = vNextBuilds.Select(x => new CompletedBuild
                        {
                            BuildName = x.Definition.Name,
                            BuildNumber = x.BuildNumber,
                            DateStarted = x.StartTime,
                            DateCompleted = x.FinishTime,
                            QueueTime = x.QueueTime,
                            RequestedBy = GetRequestedByName(x.RequestedBy.DisplayName, x.RequestedFor.DisplayName),
                            BuildStatus = x.Result.GetValueOrDefault().ToString(),
                            VNextBuild = x,
                            Url = $"{TfsBuildAnalyzerConfiguration.TfsServerPath}/{teamProjectCollection.CatalogNode.Resource.DisplayName}/{x.Project.Name}/_build/index?buildId={x.Id}&_a=summary"
                        }).ToList();
                        completedVNextBuilds = completedVNextBuilds.OrderByDescending(x => x.VNextBuild.QueueTime).ToList();

                        myCompletedBuilds = completedVNextBuilds;
                    }
                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    throw;
                }
            }, cancellationToken);

            return isSuccessful;
        }

        private static string GetRequestedByName(string requestedBy, string requestedFor)
        {
            if (string.IsNullOrEmpty(requestedFor) || requestedBy.CaseInsensitiveEquals(requestedFor)) return requestedBy;
            return $"{requestedBy} on behalf of{Environment.NewLine}{requestedFor}";
        }

        private void UpdateCompletedBuildsGrid()
        {
            var buildsToShow = myCompletedBuilds.ToList();
            if (ShowOnlyMyBuildsCheckBox.IsChecked.GetValueOrDefault())
            {
                buildsToShow = buildsToShow.Where(x => x.RequestedBy.CaseInsensitiveContains(myTfsUserName)).ToList();
            }

            BuildDetailsGrid.ItemsSource = buildsToShow;
            BuildDetailsGrid.DataContext = buildsToShow;

            CompletedBuildsStatusLabel.Content = $"Showing {buildsToShow.Count} results.";
        }

        private void RunOnUIThread(Action action)
        {
            myDispatcherObject.Invoke(() => {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            });
        }


        private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                if (dataGrid.SelectedItem is BuildType buildType)
                {
                    Clipboard.SetDataObject(buildType.BuildName);
                }
                else if (dataGrid.SelectedItem is CompletedBuild completedBuild)
                {
                    Clipboard.SetDataObject(completedBuild.BuildNumber);
                }
            }
        }

        private void AllBuildTypeSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBuildDefinitionGrids();
        }

        private void FavoriteBuildNamesGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FavoriteBuildNamesGrid.SelectedIndex == -1) return;
            if (FavoriteBuildNamesGrid.SelectedItem is BuildType buildType && myShowingCompletedBuildsForBuildType != null && !myShowingCompletedBuildsForBuildType.Equals(buildType))
            {
                ClearBuildDetailsGrid();
            }

            AllBuildNamesGrid.SelectedIndex = -1;
        }

        private void AllBuildNamesGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AllBuildNamesGrid.SelectedIndex == -1) return;
            if (AllBuildNamesGrid.SelectedItem is BuildType buildType && myShowingCompletedBuildsForBuildType != null && !myShowingCompletedBuildsForBuildType.Equals(buildType))
            {
                ClearBuildDetailsGrid();
            }

            FavoriteBuildNamesGrid.SelectedIndex = -1;
        }

        private void FavoriteBuildNamesGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FavoriteBuildNamesGrid.SelectedItem is BuildType buildType)
            {
                myShowingCompletedBuildsForBuildType = buildType;
                GetCompletedBuilds(buildType);
            }
        }

        private void AllBuildNamesGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AllBuildNamesGrid.SelectedItem is BuildType buildType)
            {
                myShowingCompletedBuildsForBuildType = buildType;
                GetCompletedBuilds(buildType);
            }
        }

        private void BuildDetailsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BuildDetailsGrid.SelectedItem is CompletedBuild completedBuild)
            {
                if (!completedBuild.BuildStatus.CaseInsensitiveEquals("Succeeded") && !completedBuild.BuildStatus.CaseInsensitiveEquals("Failed") &&
                    !completedBuild.BuildStatus.CaseInsensitiveEquals("PartiallySucceeded"))
                {
                    MessageBoxHelper.ShowWarningMessage("Build not completed.", $"This build is not in completed state.{Environment.NewLine}Cannot analyze results.{Environment.NewLine}Click build name to open status.");
                    return;
                }

                if (string.IsNullOrEmpty(completedBuild.DropLocation) && completedBuild.VNextBuild != null)
                {
                    try
                    {
                        var json = GetVNextBuildDropFolderPath(completedBuild.VNextBuild);
                        var obj = JsonConvert.DeserializeObject(json);
                        var nodes = (JArray)JObject.Parse(obj.ToString())["value"];
                        var dropFolderNode = nodes.First(x => ((JObject)x)["name"].ToString().Equals("drop"));
                        var dropFolder = dropFolderNode["resource"]["properties"]["artifactlocation"].ToString();
                        completedBuild.DropLocation = dropFolder;
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowErrorMessage("Drop folder not accessible.", "Could not get the path for drop folder for the selected build.");
                        LogHelper.LogException(ex);
                        return;
                    }
                }
                TfsBuildAnalyzerApplication.ShowBuildHistory(completedBuild.BuildName);
                AnalyzeSelectedTfsBuild(completedBuild);
            }
        }

        private static void AnalyzeSelectedTfsBuild(CompletedBuild completedBuild)
        {
            var buildInfo = new BuildInfo
            {
                BuildType = completedBuild.BuildName,
                BuildNumber = completedBuild.BuildNumber,
                BuildDropLocation = completedBuild.DropLocation,
                BuildDate = completedBuild.DateCompleted.GetValueOrDefault(),
                BuildStatus = completedBuild.BuildStatus,
                BuildUrl = completedBuild.Url
            };
            var canAnalyzeState = BuildDropFolderValidityChecker.CanAnalyze(buildInfo);

            if (canAnalyzeState == CanAnalyzeState.CanAnalyze)
            {
                var analyzeDropFolderWindow = new AnalyzeDropFolderWindow(buildInfo);
                analyzeDropFolderWindow.ShowDialog();
            }
            else if (canAnalyzeState == CanAnalyzeState.BuildExists)
            {
                BuildDropFolderValidityChecker.LoadExistingBuild(buildInfo.BuildType, buildInfo.BuildNumber);
            }
            else
            {
                BuildDropFolderValidityChecker.ShowMessageForAnalyzeState(canAnalyzeState, buildInfo);
            }
        }

        private void ToggleFavoriteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var favoriteBuildNames = UtilityMethods.CommaSeparatedStringToList(TfsBuildAnalyzerConfiguration.MyFavoriteBuilds);
            if (AllBuildNamesGrid.SelectedIndex >= 0)
            {
                if (AllBuildNamesGrid.SelectedItem is BuildType buildType)
                {
                    buildType.IsFavorite = true;
                    favoriteBuildNames.Add(buildType.BuildName);
                }
            }
            else if (FavoriteBuildNamesGrid.SelectedIndex >= 0)
            {
                if (FavoriteBuildNamesGrid.SelectedItem is BuildType buildType)
                {
                    buildType.IsFavorite = false;
                    favoriteBuildNames.Remove(buildType.BuildName);
                }
            }

            TfsBuildAnalyzerConfiguration.MyFavoriteBuilds = UtilityMethods.ListToCommaSeparatedString(favoriteBuildNames);

            UpdateBuildDefinitionGrids();
        }

        private void ReloadBuildDefinitionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            ReloadBuildDefinitionsButton.Visibility = Visibility.Collapsed;
            if (TfsBuildAnalyzerConfiguration.ProjectStructure == null)
            {
                UpdateCollectionsAndProjects();
                return;
            }

            UpdateAllBuildTypes(false);
        }

        private void OnBuildClickHandler(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            var url = textBlock.Tag.ToString();
            try
            {
                Process.Start(url);
            }
            catch
            {
                MessageBoxHelper.ShowErrorMessage("Could not open URL", $"Error opening url: '{url}'");
            }
        }

        private void ProjectCatalogComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCatalogNode = GetSelectedProjectCatalogNode();
            if (selectedCatalogNode == null) return;

            TfsBuildAnalyzerConfiguration.LastSelectedProjectCatalogName = selectedCatalogNode.Name;

            ProjectTypeComboBox.ItemsSource =
                selectedCatalogNode.ProjectInfoNodes.Select(x => new ComboBoxKeyValue(x.Id.ToString(), x.Name));
            if (ProjectTypeComboBox.Items.Count == 0) return;

            SelectProjectTypeInComboBox();
        }

        private void ProjectTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProjectTypeNode = GetSelectedProjectTypeNode();
            if (selectedProjectTypeNode == null) return;
            TfsBuildAnalyzerConfiguration.LastSelectedProjectTypeName = selectedProjectTypeNode.Name;

            UpdateAllBuildTypes(true);
        }



        private static string GetVNextBuildDropFolderPath(Build build)
        {
            return GetHttpResponse($"{build.Url}/artifacts");
        }

        private static string GetHttpResponse(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.UseDefaultCredentials = true;
            request.PreAuthenticate = true;
            request.Timeout = 25 * 1000;
            request.Credentials = CredentialCache.DefaultCredentials;

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null) return null;
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private void ClearBuildDetailsGrid()
        {
            myShowingCompletedBuildsForBuildType = null;
            myCompletedBuilds = new List<CompletedBuild>();
            BuildDetailsGrid.ItemsSource = myCompletedBuilds;
            BuildDetailsGrid.DataContext = myCompletedBuilds;
            CompletedBuildsStatusLabel.Content = $"Showing {myCompletedBuilds.Count} results.";
        }

        private void SelectProjectCatalogInComboBox()
        {
            var itemToSelect = TfsBuildAnalyzerConfiguration.ProjectStructure.ProjectCatalogNodeList.FirstOrDefault(x =>
                x.Name.Equals(TfsBuildAnalyzerConfiguration.LastSelectedProjectCatalogName));
            if (itemToSelect != null)
            {
                var match = ProjectCatalogComboBox.Items.Cast<ComboBoxKeyValue>().FirstOrDefault(x => x.ToString().Equals(itemToSelect.Name));
                if (match != null)
                {
                    var index = ProjectCatalogComboBox.Items.IndexOf(match);
                    if (index >= 0)
                    {
                        ProjectCatalogComboBox.SelectedIndex = index;
                        return;
                    }
                }
            }
            ProjectCatalogComboBox.SelectedIndex = 0;
        }

        private void SelectProjectTypeInComboBox()
        {
            var selectedCatalogNode = GetSelectedProjectCatalogNode();
            var itemToSelect = selectedCatalogNode?.ProjectInfoNodes.FirstOrDefault(x => x.Name.Equals(TfsBuildAnalyzerConfiguration.LastSelectedProjectTypeName));
            if (itemToSelect != null)
            {
                var match = ProjectTypeComboBox.Items.Cast<ComboBoxKeyValue>().FirstOrDefault(x => x.ToString().Equals(itemToSelect.Name));
                if (match != null)
                {
                    var index = ProjectTypeComboBox.Items.IndexOf(match);
                    if (index >= 0)
                    {
                        ProjectTypeComboBox.SelectedIndex = index;
                        return;
                    }
                }
            }
            ProjectTypeComboBox.SelectedIndex = 0;
        }

        private ProjectCatalogNode GetSelectedProjectCatalogNode()
        {
            if (TfsBuildAnalyzerConfiguration.ProjectStructure == null) return null;

            if (!(ProjectCatalogComboBox.SelectedItem is ComboBoxKeyValue projectCatalogSelectedItem)) return null;

            var selectedCatalogNodeId = Guid.Parse(projectCatalogSelectedItem.Key);
            return TfsBuildAnalyzerConfiguration.ProjectStructure.ProjectCatalogNodeList.First(x => x.Id.Equals(selectedCatalogNodeId));
        }

        private ProjectInfoNode GetSelectedProjectTypeNode()
        {
            var selectedCatalogNode = GetSelectedProjectCatalogNode();
            if (selectedCatalogNode == null) return null;

            if (!(ProjectTypeComboBox.SelectedItem is ComboBoxKeyValue projectTypeSelectedItem)) return null;
            var selectedProjectNodeId = Guid.Parse(projectTypeSelectedItem.Key);

            return selectedCatalogNode.ProjectInfoNodes.First(x => x.Id.Equals(selectedProjectNodeId));
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            var showingCompletedBuildsForBuildType = myShowingCompletedBuildsForBuildType;
            UpdateCompletedBuildsGrid();
            myShowingCompletedBuildsForBuildType = showingCompletedBuildsForBuildType;
        }
    }
}
