using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Serilog;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.ViewModels
{
    /// <summary>
    /// 配方管理视图模型
    /// </summary>
    public class RecipeManagementViewModel : DisposableViewModel
    {
        private readonly RecipeService _recipeService;
        private readonly MaterialService _materialService;
        private readonly PlcRecipeDownloadService _plcDownloadService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<RecipeBase> _recipes = new();
        private ObservableCollection<WeighingRecipeLine> _weighingLines = new();
        private ObservableCollection<MixingRecipeStep> _mixingSteps = new();
        private ObservableCollection<RecipeVersion> _versions = new();
        private ObservableCollection<RecipeDownloadLog> _downloadLogs = new();
        private ObservableCollection<BatchRecord> _batches = new();

        private RecipeBase? _selectedRecipe;
        private WeighingRecipeLine? _selectedWeighingLine;
        private MixingRecipeStep? _selectedMixingStep;
        private RecipeVersion? _selectedVersion;

        private string _searchText = "";
        private RecipeType? _recipeTypeFilter;
        private RecipeStatus? _statusFilter;
        private int _page = 1;
        private int _pageSize = 20;
        private int _totalCount;
        private bool _isLoading;
        private string _statusMessage = "就绪";

        #endregion

        #region 属性

        public ObservableCollection<RecipeBase> Recipes => _recipes;
        public ObservableCollection<WeighingRecipeLine> WeighingLines => _weighingLines;
        public ObservableCollection<MixingRecipeStep> MixingSteps => _mixingSteps;
        public ObservableCollection<RecipeVersion> Versions => _versions;
        public ObservableCollection<RecipeDownloadLog> DownloadLogs => _downloadLogs;
        public ObservableCollection<BatchRecord> Batches => _batches;
        public ObservableCollection<Material> WeighableMaterials { get; } = new();

        public RecipeBase? SelectedRecipe
        {
            get => _selectedRecipe;
            set => SetProperty(ref _selectedRecipe, value, onChanged: OnRecipeSelected);
        }

        public WeighingRecipeLine? SelectedWeighingLine
        {
            get => _selectedWeighingLine;
            set => SetProperty(ref _selectedWeighingLine, value);
        }

        public MixingRecipeStep? SelectedMixingStep
        {
            get => _selectedMixingStep;
            set => SetProperty(ref _selectedMixingStep, value);
        }

        public RecipeVersion? SelectedVersion
        {
            get => _selectedVersion;
            set => SetProperty(ref _selectedVersion, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value, onChanged: () => { _page = 1; _ = LoadRecipesAsync(); });
        }

        public RecipeType? RecipeTypeFilter
        {
            get => _recipeTypeFilter;
            set => SetProperty(ref _recipeTypeFilter, value, onChanged: () => { _page = 1; _ = LoadRecipesAsync(); });
        }

        public RecipeStatus? StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value, onChanged: () => { _page = 1; _ = LoadRecipesAsync(); });
        }

        public int Page
        {
            get => _page;
            set => SetProperty(ref _page, value, onChanged: () => _ = LoadRecipesAsync());
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value, onChanged: () => { _page = 1; _ = LoadRecipesAsync(); });
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<RecipeType> RecipeTypes { get; } = new(Enum.GetValues(typeof(RecipeType)).Cast<RecipeType>());
        public ObservableCollection<RecipeStatus> RecipeStatuses { get; } = new(Enum.GetValues(typeof(RecipeStatus)).Cast<RecipeStatus>());
        public ObservableCollection<StepType> StepTypes { get; } = new(Enum.GetValues(typeof(StepType)).Cast<StepType>());
        public ObservableCollection<FeederType> FeederTypes { get; } = new(Enum.GetValues(typeof(FeederType)).Cast<FeederType>());
        public ObservableCollection<WeighingMode> WeighingModes { get; } = new(Enum.GetValues(typeof(WeighingMode)).Cast<WeighingMode>());
        public ObservableCollection<MixingProcessType> MixingProcessTypes { get; } = new(Enum.GetValues(typeof(MixingProcessType)).Cast<MixingProcessType>());
        public ObservableCollection<DownloadStatus> DownloadStatuses { get; } = new(Enum.GetValues(typeof(DownloadStatus)).Cast<DownloadStatus>());
        public ObservableCollection<BatchStatus> BatchStatuses { get; } = new(Enum.GetValues(typeof(BatchStatus)).Cast<BatchStatus>());

        #endregion

        #region 编辑状态

        private RecipeBase? _editingRecipe;
        private WeighingRecipeLine? _editingWeighingLine;
        private MixingRecipeStep? _editingMixingStep;

        private bool _isEditingRecipe;
        private bool _isEditingWeighingLine;
        private bool _isEditingMixingStep;

        public RecipeBase? EditingRecipe
        {
            get => _editingRecipe;
            set => SetProperty(ref _editingRecipe, value);
        }

        public WeighingRecipeLine? EditingWeighingLine
        {
            get => _editingWeighingLine;
            set => SetProperty(ref _editingWeighingLine, value);
        }

        public MixingRecipeStep? EditingMixingStep
        {
            get => _editingMixingStep;
            set => SetProperty(ref _editingMixingStep, value);
        }

        public bool IsEditingRecipe
        {
            get => _isEditingRecipe;
            set => SetProperty(ref _isEditingRecipe, value);
        }

public bool IsEditingWeighingLine
        {
            get => _isEditingWeighingLine;
            set => SetProperty(ref _isEditingWeighingLine, value);
        }

        public bool IsEditingMixingStep
        {
            get => _isEditingMixingStep;
            set => SetProperty(ref _isEditingMixingStep, value);
        }

        #endregion

        #region 命令

        public ICommand LoadCommand { get; }
        public ICommand NewRecipeCommand { get; }
        public ICommand EditRecipeCommand { get; }
        public ICommand DeleteRecipeCommand { get; }
        public ICommand SaveRecipeCommand { get; }
        public ICommand CancelRecipeCommand { get; }
        public ICommand CopyRecipeCommand { get; }

        public ICommand SubmitForReviewCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand ReleaseCommand { get; }
        public ICommand ArchiveCommand { get; }
        public ICommand ObsoleteCommand { get; }

        public ICommand NewVersionCommand { get; }
        public ICommand SetCurrentVersionCommand { get; }

        public ICommand AddWeighingLineCommand { get; }
        public ICommand EditWeighingLineCommand { get; }
        public ICommand DeleteWeighingLineCommand { get; }
        public ICommand SaveWeighingLineCommand { get; }
        public ICommand CancelWeighingLineCommand { get; }
        public ICommand MoveWeighingLineUpCommand { get; }
        public ICommand MoveWeighingLineDownCommand { get; }

        public ICommand AddMixingStepCommand { get; }
        public ICommand EditMixingStepCommand { get; }
        public ICommand DeleteMixingStepCommand { get; }
        public ICommand SaveMixingStepCommand { get; }
        public ICommand CancelMixingStepCommand { get; }
        public ICommand MoveMixingStepUpCommand { get; }
        public ICommand MoveMixingStepDownCommand { get; }

        public ICommand DownloadToPlcCommand { get; }
        public ICommand VerifyFromPlcCommand { get; }
        public ICommand ViewDownloadLogsCommand { get; }

        public ICommand CreateBatchCommand { get; }
        public ICommand UpdateBatchStatusCommand { get; }
        public ICommand ViewBatchDetailsCommand { get; }

        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion

        public RecipeManagementViewModel(
            RecipeService recipeService,
            MaterialService materialService,
            PlcRecipeDownloadService plcDownloadService)
        {
            _recipeService = recipeService;
            _materialService = materialService;
            _plcDownloadService = plcDownloadService;
            _logger = LogManager.CreateModuleLogger(nameof(RecipeManagementViewModel));

            // 初始化命令
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            NewRecipeCommand = new RelayCommand(() => StartNewRecipe());
            EditRecipeCommand = new RelayCommand(() => StartEditRecipe(), () => SelectedRecipe != null);
            DeleteRecipeCommand = new AsyncRelayCommand(DeleteRecipeAsync, () => SelectedRecipe != null && !IsEditingRecipe);
            SaveRecipeCommand = new AsyncRelayCommand(SaveRecipeAsync, () => IsEditingRecipe);
            CancelRecipeCommand = new RelayCommand(CancelRecipeEdit);
            CopyRecipeCommand = new AsyncRelayCommand(CopyRecipeAsync, () => SelectedRecipe != null);

            SubmitForReviewCommand = new AsyncRelayCommand(SubmitForReviewAsync, () => SelectedRecipe?.Status == RecipeStatus.Draft);
            ApproveCommand = new AsyncRelayCommand(ApproveAsync, () => SelectedRecipe?.Status == RecipeStatus.PendingReview);
            RejectCommand = new AsyncRelayCommand(RejectAsync, () => SelectedRecipe?.Status == RecipeStatus.PendingReview);
            ReleaseCommand = new AsyncRelayCommand(ReleaseAsync, () => SelectedRecipe?.Status == RecipeStatus.Approved);
            ArchiveCommand = new AsyncRelayCommand(ArchiveAsync, () => SelectedRecipe?.Status == RecipeStatus.Released);
            ObsoleteCommand = new AsyncRelayCommand(ObsoleteAsync, () => SelectedRecipe?.Status != RecipeStatus.Obsolete);

            NewVersionCommand = new AsyncRelayCommand(NewVersionAsync, () => SelectedRecipe != null);
            SetCurrentVersionCommand = new AsyncRelayCommand(SetCurrentVersionAsync, () => SelectedVersion != null);

            AddWeighingLineCommand = new RelayCommand(() => StartNewWeighingLine());
            EditWeighingLineCommand = new RelayCommand(() => StartEditWeighingLine(), () => SelectedWeighingLine != null);
            DeleteWeighingLineCommand = new AsyncRelayCommand(DeleteWeighingLineAsync, () => SelectedWeighingLine != null);
            SaveWeighingLineCommand = new AsyncRelayCommand(SaveWeighingLineAsync, () => IsEditingWeighingLine);
            CancelWeighingLineCommand = new RelayCommand(CancelWeighingLineEdit);
            MoveWeighingLineUpCommand = new AsyncRelayCommand(MoveWeighingLineUpAsync, () => SelectedWeighingLine != null && SelectedWeighingLine.LineNumber > 1);
            MoveWeighingLineDownCommand = new AsyncRelayCommand(MoveWeighingLineDownAsync, () => SelectedWeighingLine != null);

            AddMixingStepCommand = new RelayCommand(() => StartNewMixingStep());
            EditMixingStepCommand = new RelayCommand(() => StartEditMixingStep(), () => SelectedMixingStep != null);
            DeleteMixingStepCommand = new AsyncRelayCommand(DeleteMixingStepAsync, () => SelectedMixingStep != null);
            SaveMixingStepCommand = new AsyncRelayCommand(SaveMixingStepAsync, () => IsEditingMixingStep);
            CancelMixingStepCommand = new RelayCommand(CancelMixingStepEdit);
            MoveMixingStepUpCommand = new AsyncRelayCommand(MoveMixingStepUpAsync, () => SelectedMixingStep != null && SelectedMixingStep.StepNumber > 1);
            MoveMixingStepDownCommand = new AsyncRelayCommand(MoveMixingStepDownAsync, () => SelectedMixingStep != null);

            DownloadToPlcCommand = new AsyncRelayCommand(DownloadToPlcAsync, () => SelectedRecipe?.Status == RecipeStatus.Released);
            VerifyFromPlcCommand = new AsyncRelayCommand(VerifyFromPlcAsync, () => SelectedRecipe != null);
            ViewDownloadLogsCommand = new RelayCommand(ViewDownloadLogs);

            CreateBatchCommand = new AsyncRelayCommand(CreateBatchAsync, () => SelectedRecipe?.Status == RecipeStatus.Released);
            UpdateBatchStatusCommand = new AsyncRelayCommand(UpdateBatchStatusAsync);
            ViewBatchDetailsCommand = new RelayCommand(ViewBatchDetails);

            PrevPageCommand = new RelayCommand(() => Page--, () => Page > 1);
            NextPageCommand = new RelayCommand(() => Page++, () => Page < TotalPages);
            FirstPageCommand = new RelayCommand(() => Page = 1, () => Page > 1);
            LastPageCommand = new RelayCommand(() => Page = TotalPages, () => Page < TotalPages);

            // 自动加载
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await LoadRecipesAsync();
            await LoadWeighableMaterialsAsync();
        }

        private async Task LoadWeighableMaterialsAsync()
        {
            try
            {
                var materials = await _materialService.GetWeighableMaterialsAsync();
                WeighableMaterials.Clear();
                foreach (var m in materials) WeighableMaterials.Add(m);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载可称量物料失败");
            }
        }

        private async Task LoadRecipesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载配方...";

                var filter = new RecipeFilter
                {
                    Keyword = SearchText,
                    RecipeType = RecipeTypeFilter,
                    Status = StatusFilter
                };

                var (items, total) = await _recipeService.GetRecipesPagedAsync(Page, PageSize, filter);
                _recipes.Clear();
                foreach (var r in items) _recipes.Add(r);
                TotalCount = total;

                OnPropertyChanged(nameof(TotalPages));

                StatusMessage = $"配方: {_recipes.Count} / {total} 项";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载配方失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnRecipeSelected()
        {
            _weighingLines.Clear();
            _mixingSteps.Clear();
            _versions.Clear();
            _downloadLogs.Clear();
            _batches.Clear();

            if (SelectedRecipe == null) return;

            try
            {
                // 加载版本
                var versions = await _recipeService.GetVersionsAsync(SelectedRecipe.Id);
                foreach (var v in versions) _versions.Add(v);

                if (SelectedRecipe is WeighingRecipe)
                {
                    var lines = await _recipeService.GetWeighingLinesAsync(SelectedRecipe.Id);
                    foreach (var l in lines) _weighingLines.Add(l);
                }
                else if (SelectedRecipe is MixingRecipe)
                {
                    var steps = await _recipeService.GetMixingStepsAsync(SelectedRecipe.Id);
                    foreach (var s in steps) _mixingSteps.Add(s);
                }

                // 加载下发日志
                var logs = await _recipeService.GetDownloadHistoryAsync(SelectedRecipe.Id);
                foreach (var log in logs) _downloadLogs.Add(log);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载配方详情失败");
            }
        }

        #region 配方编辑

        private void StartNewRecipe()
        {
            EditingRecipe = new WeighingRecipe
            {
                Status = RecipeStatus.Draft,
                CurrentVersion = "1.0",
                BatchSize = 1000,
                YieldRate = 100
            };
            IsEditingRecipe = true;
            SelectedRecipe = null;
        }

        private void StartEditRecipe()
        {
            if (SelectedRecipe == null) return;
            EditingRecipe = CloneRecipe(SelectedRecipe);
            IsEditingRecipe = true;
        }

        private void CancelRecipeEdit()
        {
            EditingRecipe = null;
            IsEditingRecipe = false;
        }

        private async Task SaveRecipeAsync()
        {
            if (EditingRecipe == null) return;

            try
            {
                if (EditingRecipe.Id == 0)
                {
                    await _recipeService.CreateRecipeAsync(EditingRecipe, "CurrentUser");
                }
                else
                {
                    await _recipeService.UpdateRecipeAsync(EditingRecipe, "CurrentUser");
                }

                CancelRecipeEdit();
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存配方失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteRecipeAsync()
        {
            if (SelectedRecipe == null) return;

            var result = MessageBox.Show(
                $"确定要删除配方 '{SelectedRecipe.Name}' 吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _recipeService.DeleteRecipeAsync(SelectedRecipe.Id, "CurrentUser");
                    await LoadRecipesAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "删除配方失败");
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task CopyRecipeAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                var newCode = $"{SelectedRecipe.Code}_COPY";
                var newName = $"{SelectedRecipe.Name} (副本)";
                await _recipeService.CopyRecipeAsync(SelectedRecipe.Id, newCode, newName, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "复制配方失败");
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RecipeBase CloneRecipe(RecipeBase source)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(source, new Newtonsoft.Json.JsonSerializerSettings
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            });
            return Newtonsoft.Json.JsonConvert.DeserializeObject<RecipeBase>(json)!;
        }

        #endregion

        #region 状态流转

        private async Task SubmitForReviewAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                await _recipeService.SubmitForReviewAsync(SelectedRecipe.Id, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "提交审核失败");
                MessageBox.Show($"提交失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApproveAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                await _recipeService.ApproveAsync(SelectedRecipe.Id, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "审核通过失败");
                MessageBox.Show($"审核失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RejectAsync()
        {
            if (SelectedRecipe == null) return;

            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入驳回原因:", "驳回配方", "", -1, -1);

            if (string.IsNullOrWhiteSpace(reason)) return;

            try
            {
                await _recipeService.RejectAsync(SelectedRecipe.Id, reason, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "驳回失败");
                MessageBox.Show($"驳回失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ReleaseAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                await _recipeService.ReleaseAsync(SelectedRecipe.Id, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "发布失败");
                MessageBox.Show($"发布失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ArchiveAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                await _recipeService.ArchiveAsync(SelectedRecipe.Id, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "归档失败");
                MessageBox.Show($"归档失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ObsoleteAsync()
        {
            if (SelectedRecipe == null) return;

            var result = MessageBox.Show(
                "确定要作废此配方吗？作废后不可恢复。",
                "确认作废", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _recipeService.ObsoleteAsync(SelectedRecipe.Id, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "作废失败");
                MessageBox.Show($"作废失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 版本管理

        private async Task NewVersionAsync()
        {
            if (SelectedRecipe == null) return;

            var version = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入新版本号:", "新建版本", 
                GetNextVersion(SelectedRecipe.CurrentVersion), -1, -1);

            if (string.IsNullOrWhiteSpace(version)) return;

            var changeDesc = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入变更描述:", "变更说明", "", -1, -1);

            try
            {
                await _recipeService.CreateVersionAsync(SelectedRecipe.Id, version, changeDesc, "CurrentUser");
                // 重新加载版本
                var versions = await _recipeService.GetVersionsAsync(SelectedRecipe.Id);
                _versions.Clear();
                foreach (var v in versions) _versions.Add(v);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "新建版本失败");
                MessageBox.Show($"新建版本失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetNextVersion(string current)
        {
            if (Version.TryParse(current, out var v))
            {
                return $"{v.Major}.{v.Minor + 1}";
            }
            return "1.1";
        }

        private async Task SetCurrentVersionAsync()
        {
            if (SelectedRecipe == null || SelectedVersion == null) return;

            try
            {
                await _recipeService.SetCurrentVersionAsync(SelectedRecipe.Id, SelectedVersion.Id, "CurrentUser");
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "设置当前版本失败");
                MessageBox.Show($"设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 称量配方明细

        private void StartNewWeighingLine()
        {
            if (SelectedRecipe is not WeighingRecipe) return;

            EditingWeighingLine = new WeighingRecipeLine
            {
                RecipeId = SelectedRecipe.Id,
                LineNumber = _weighingLines.Count + 1,
                TolerancePositive = 1.0m,
                ToleranceNegative = 1.0m,
                FeederType = FeederType.Auto,
                IsCritical = true
            };
            IsEditingWeighingLine = true;
            SelectedWeighingLine = null;
        }

        private void StartEditWeighingLine()
        {
            if (SelectedWeighingLine == null) return;
            EditingWeighingLine = CloneWeighingLine(SelectedWeighingLine);
            IsEditingWeighingLine = true;
        }

        private void CancelWeighingLineEdit()
        {
            EditingWeighingLine = null;
            IsEditingWeighingLine = false;
        }

        private async Task SaveWeighingLineAsync()
        {
            if (EditingWeighingLine == null || SelectedRecipe == null) return;

            try
            {
                if (EditingWeighingLine.Id == 0)
                {
                    await _recipeService.AddWeighingLineAsync(SelectedRecipe.Id, EditingWeighingLine, "CurrentUser");
                }
                else
                {
                    await _recipeService.UpdateWeighingLineAsync(EditingWeighingLine, "CurrentUser");
                }

                CancelWeighingLineEdit();
                // 重新加载明细
                var lines = await _recipeService.GetWeighingLinesAsync(SelectedRecipe.Id);
                _weighingLines.Clear();
                foreach (var l in lines) _weighingLines.Add(l);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存称量明细失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteWeighingLineAsync()
        {
            if (SelectedWeighingLine == null) return;

            var result = MessageBox.Show(
                $"确定要删除第 {SelectedWeighingLine.LineNumber} 行吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _recipeService.DeleteWeighingLineAsync(SelectedWeighingLine.Id, "CurrentUser");
                    var lines = await _recipeService.GetWeighingLinesAsync(SelectedRecipe!.Id);
                    _weighingLines.Clear();
                    foreach (var l in lines) _weighingLines.Add(l);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "删除称量明细失败");
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task MoveWeighingLineUpAsync()
        {
            if (SelectedWeighingLine == null || SelectedRecipe == null) return;

            var lineIds = _weighingLines.OrderBy(l => l.LineNumber).Select(l => l.Id).ToList();
            var index = lineIds.IndexOf(SelectedWeighingLine.Id);
            if (index <= 0) return;

            (lineIds[index], lineIds[index - 1]) = (lineIds[index - 1], lineIds[index]);

            try
            {
                await _recipeService.ReorderWeighingLinesAsync(SelectedRecipe.Id, lineIds, "CurrentUser");
                var lines = await _recipeService.GetWeighingLinesAsync(SelectedRecipe.Id);
                _weighingLines.Clear();
                foreach (var l in lines) _weighingLines.Add(l);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "上移失败");
            }
        }

        private async Task MoveWeighingLineDownAsync()
        {
            if (SelectedWeighingLine == null || SelectedRecipe == null) return;

            var lineIds = _weighingLines.OrderBy(l => l.LineNumber).Select(l => l.Id).ToList();
            var index = lineIds.IndexOf(SelectedWeighingLine.Id);
            if (index >= lineIds.Count - 1) return;

            (lineIds[index], lineIds[index + 1]) = (lineIds[index + 1], lineIds[index]);

            try
            {
                await _recipeService.ReorderWeighingLinesAsync(SelectedRecipe.Id, lineIds, "CurrentUser");
                var lines = await _recipeService.GetWeighingLinesAsync(SelectedRecipe.Id);
                _weighingLines.Clear();
                foreach (var l in lines) _weighingLines.Add(l);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下移失败");
            }
        }

        private WeighingRecipeLine CloneWeighingLine(WeighingRecipeLine source)
        {
            return new WeighingRecipeLine
            {
                Id = source.Id,
                RecipeId = source.RecipeId,
                LineNumber = source.LineNumber,
                MaterialId = source.MaterialId,
                TargetWeight = source.TargetWeight,
                TolerancePositive = source.TolerancePositive,
                ToleranceNegative = source.ToleranceNegative,
                WeighingOrder = source.WeighingOrder,
                IsCritical = source.IsCritical,
                Remark = source.Remark,
                FeederType = source.FeederType,
                PlcTagIndex = source.PlcTagIndex,
                PlcWeightTag = source.PlcWeightTag,
                PlcMaterialTag = source.PlcMaterialTag
            };
        }

        #endregion

        #region 混炼配方步骤

        private void StartNewMixingStep()
        {
            if (SelectedRecipe is not MixingRecipe) return;

            EditingMixingStep = new MixingRecipeStep
            {
                RecipeId = SelectedRecipe.Id,
                StepNumber = _mixingSteps.Count + 1,
                StepType = StepType.AddMaterial,
                Duration = 60,
                RotorSpeed = 60
            };
            IsEditingMixingStep = true;
            SelectedMixingStep = null;
        }

        private void StartEditMixingStep()
        {
            if (SelectedMixingStep == null) return;
            EditingMixingStep = CloneMixingStep(SelectedMixingStep);
            IsEditingMixingStep = true;
        }

        private void CancelMixingStepEdit()
        {
            EditingMixingStep = null;
            IsEditingMixingStep = false;
        }

        private async Task SaveMixingStepAsync()
        {
            if (EditingMixingStep == null || SelectedRecipe == null) return;

            try
            {
                if (EditingMixingStep.Id == 0)
                {
                    await _recipeService.AddMixingStepAsync(SelectedRecipe.Id, EditingMixingStep, "CurrentUser");
                }
                else
                {
                    await _recipeService.UpdateMixingStepAsync(EditingMixingStep, "CurrentUser");
                }

                CancelMixingStepEdit();
                var steps = await _recipeService.GetMixingStepsAsync(SelectedRecipe.Id);
                _mixingSteps.Clear();
                foreach (var s in steps) _mixingSteps.Add(s);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存混炼步骤失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteMixingStepAsync()
        {
            if (SelectedMixingStep == null) return;

            var result = MessageBox.Show(
                $"确定要删除步骤 {SelectedMixingStep.StepNumber} 吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _recipeService.DeleteMixingStepAsync(SelectedMixingStep.Id, "CurrentUser");
                    var steps = await _recipeService.GetMixingStepsAsync(SelectedRecipe!.Id);
                    _mixingSteps.Clear();
                    foreach (var s in steps) _mixingSteps.Add(s);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "删除混炼步骤失败");
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task MoveMixingStepUpAsync()
        {
            if (SelectedMixingStep == null || SelectedRecipe == null) return;

            var stepIds = _mixingSteps.OrderBy(s => s.StepNumber).Select(s => s.Id).ToList();
            var index = stepIds.IndexOf(SelectedMixingStep.Id);
            if (index <= 0) return;

            (stepIds[index], stepIds[index - 1]) = (stepIds[index - 1], stepIds[index]);

            try
            {
                await _recipeService.ReorderMixingStepsAsync(SelectedRecipe.Id, stepIds, "CurrentUser");
                var steps = await _recipeService.GetMixingStepsAsync(SelectedRecipe.Id);
                _mixingSteps.Clear();
                foreach (var s in steps) _mixingSteps.Add(s);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "上移失败");
            }
        }

        private async Task MoveMixingStepDownAsync()
        {
            if (SelectedMixingStep == null || SelectedRecipe == null) return;

            var stepIds = _mixingSteps.OrderBy(s => s.StepNumber).Select(s => s.Id).ToList();
            var index = stepIds.IndexOf(SelectedMixingStep.Id);
            if (index >= stepIds.Count - 1) return;

            (stepIds[index], stepIds[index + 1]) = (stepIds[index + 1], stepIds[index]);

            try
            {
                await _recipeService.ReorderMixingStepsAsync(SelectedRecipe.Id, stepIds, "CurrentUser");
                var steps = await _recipeService.GetMixingStepsAsync(SelectedRecipe.Id);
                _mixingSteps.Clear();
                foreach (var s in steps) _mixingSteps.Add(s);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下移失败");
            }
        }

        private MixingRecipeStep CloneMixingStep(MixingRecipeStep source)
        {
            return new MixingRecipeStep
            {
                Id = source.Id,
                RecipeId = source.RecipeId,
                StepNumber = source.StepNumber,
                StepType = source.StepType,
                MaterialId = source.MaterialId,
                Amount = source.Amount,
                AddTime = source.AddTime,
                Duration = source.Duration,
                TargetTemp = source.TargetTemp,
                RotorSpeed = source.RotorSpeed,
                RamPressure = source.RamPressure,
                IsAutoDump = source.IsAutoDump,
                WaitForTemp = source.WaitForTemp,
                Condition = source.Condition,
                Remark = source.Remark,
                PlcStepIndex = source.PlcStepIndex,
                PlcCommandTag = source.PlcCommandTag,
                PlcParamTags = source.PlcParamTags
            };
        }

        #endregion

        #region PLC 下发

        private async Task DownloadToPlcAsync()
        {
            if (SelectedRecipe == null) return;

            var plcName = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入目标 PLC 名称:", "下发到 PLC", "Siemens_S7_1500_Main", -1, -1);

            if (string.IsNullOrWhiteSpace(plcName)) return;

            try
            {
                StatusMessage = $"正在下发到 {plcName}...";
                IsLoading = true;

                var result = (TwoD5DMonitoring.Services.DownloadResult)null;


                if (SelectedRecipe is WeighingRecipe)
                {
                    result = await _plcDownloadService.DownloadWeighingRecipeAsync(
                        SelectedRecipe.Id, plcName, "CurrentUser");
                }
                else
                {
                    result = await _plcDownloadService.DownloadMixingRecipeAsync(
                        SelectedRecipe.Id, plcName, "CurrentUser");
                }

                if (result.Success)
                {
                    MessageBox.Show("下发成功!", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    // 刷新下发日志
                    var logs = await _recipeService.GetDownloadHistoryAsync(SelectedRecipe.Id);
                    _downloadLogs.Clear();
                    foreach (var l in logs) _downloadLogs.Add(l);
                }
                else
                {
                    MessageBox.Show($"下发失败: {result.ErrorMessage}", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                StatusMessage = result.Success ? "下发成功" : $"下发失败: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "PLC下发异常");
                MessageBox.Show($"下发异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = $"下发异常: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task VerifyFromPlcAsync()
        {
            if (SelectedRecipe == null) return;

            var plcName = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入目标 PLC 名称:", "从 PLC 校验", "Siemens_S7_1500_Main", -1, -1);

            if (string.IsNullOrWhiteSpace(plcName)) return;

            try
            {
                StatusMessage = $"正在从 {plcName} 校验...";
                IsLoading = true;

                var result = await _plcDownloadService.VerifyRecipeFromPlcAsync(SelectedRecipe.Id, plcName);

                if (result.Success)
                {
                    MessageBox.Show("校验通过!", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"校验失败:\n{result.ErrorMessage}", "失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                StatusMessage = result.Success ? "校验通过" : $"校验失败: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "PLC校验异常");
                MessageBox.Show($"校验异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = $"校验异常: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ViewDownloadLogs()
        {
            if (SelectedRecipe == null) return;
            // TODO: 打开下发日志详细窗口
            MessageBox.Show("下发日志查看功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 批次管理

        private async Task CreateBatchAsync()
        {
            if (SelectedRecipe == null) return;

            var mixerIdStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入混炼机 ID:", "创建批次", "1", -1, -1);
            if (!int.TryParse(mixerIdStr, out var mixerId)) return;

            var scaleIdStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入秤 ID:", "创建批次", "1", -1, -1);
            if (!int.TryParse(scaleIdStr, out var scaleId)) return;

            try
            {
                var batch = await _recipeService.CreateBatchAsync(SelectedRecipe.Id, mixerId, scaleId, "CurrentUser");
                MessageBox.Show($"批次创建成功: {batch.BatchNumber}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "创建批次失败");
                MessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateBatchStatusAsync()
        {
            // TODO: 实现批次状态更新
            MessageBox.Show("批次状态更新待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewBatchDetails()
        {
            // TODO: 打开批次详情窗口
            MessageBox.Show("批次详情查看待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
