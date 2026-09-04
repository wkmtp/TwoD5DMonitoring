using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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
    /// 物料管理视图模型
    /// </summary>
    public class MaterialManagementViewModel : DisposableViewModel
    {
        private readonly MaterialService _materialService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<MaterialCategory> _categories = new();
        private ObservableCollection<Material> _materials = new();
        private ObservableCollection<MaterialProperty> _materialProperties = new();
        private ObservableCollection<TankMaterial> _tankMaterials = new();
        private ObservableCollection<TankMaterialHistory> _tankMaterialHistory = new();

        private MaterialCategory? _selectedCategory;
        private Material? _selectedMaterial;
        private TankMaterial? _selectedTankMaterial;

        private string _categorySearchText = "";
        private string _materialSearchText = "";
        private int _materialPage = 1;
        private int _materialPageSize = 50;
        private int _materialTotalCount;
        private MaterialType? _materialTypeFilter;
        private MaterialStatus? _materialStatusFilter;
        private bool _isLoading;
        private string _statusMessage = "就绪";

        #endregion

        #region 属性

        public ObservableCollection<MaterialCategory> Categories => _categories;
        public ObservableCollection<Material> Materials => _materials;
        public ObservableCollection<MaterialProperty> MaterialProperties => _materialProperties;
        public ObservableCollection<TankMaterial> TankMaterials => _tankMaterials;
        public ObservableCollection<TankMaterialHistory> TankMaterialHistory => _tankMaterialHistory;

        public MaterialCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value, onChanged: OnCategorySelected);
        }

        public Material? SelectedMaterial
        {
            get => _selectedMaterial;
            set => SetProperty(ref _selectedMaterial, value, onChanged: OnMaterialSelected);
        }

        public TankMaterial? SelectedTankMaterial
        {
            get => _selectedTankMaterial;
            set => SetProperty(ref _selectedTankMaterial, value);
        }

        public string CategorySearchText
        {
            get => _categorySearchText;
            set => SetProperty(ref _categorySearchText, value, onChanged: () => _ = LoadCategoriesAsync());
        }

        public string MaterialSearchText
        {
            get => _materialSearchText;
            set => SetProperty(ref _materialSearchText, value, onChanged: () => _ = LoadMaterialsAsync());
        }

        public int MaterialPage
        {
            get => _materialPage;
            set => SetProperty(ref _materialPage, value, onChanged: () => _ = LoadMaterialsAsync());
        }

        public int MaterialPageSize
        {
            get => _materialPageSize;
            set => SetProperty(ref _materialPageSize, value, onChanged: () => { _materialPage = 1; _ = LoadMaterialsAsync(); });
        }

        public int MaterialTotalCount
        {
            get => _materialTotalCount;
            set => SetProperty(ref _materialTotalCount, value);
        }

        public int MaterialTotalPages => Math.Max(1, (int)Math.Ceiling((double)MaterialTotalCount / MaterialPageSize));

        public MaterialType? MaterialTypeFilter
        {
            get => _materialTypeFilter;
            set => SetProperty(ref _materialTypeFilter, value, onChanged: () => { _materialPage = 1; _ = LoadMaterialsAsync(); });
        }

        public MaterialStatus? MaterialStatusFilter
        {
            get => _materialStatusFilter;
            set => SetProperty(ref _materialStatusFilter, value, onChanged: () => { _materialPage = 1; _ = LoadMaterialsAsync(); });
        }

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

        public ObservableCollection<MaterialType> MaterialTypes { get; } = new(Enum.GetValues(typeof(MaterialType)).Cast<MaterialType>());
        public ObservableCollection<MaterialStatus> MaterialStatuses { get; } = new(Enum.GetValues(typeof(MaterialStatus)).Cast<MaterialStatus>());
        public ObservableCollection<FeederType> FeederTypes { get; } = new(Enum.GetValues(typeof(FeederType)).Cast<FeederType>());

        #endregion

        #region 命令

        public ICommand LoadCommand { get; }
        public ICommand NewCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand SaveCategoryCommand { get; }
        public ICommand CancelCategoryCommand { get; }

        public ICommand NewMaterialCommand { get; }
        public ICommand EditMaterialCommand { get; }
        public ICommand DeleteMaterialCommand { get; }
        public ICommand SaveMaterialCommand { get; }
        public ICommand CancelMaterialCommand { get; }
        public ICommand CopyMaterialCommand { get; }
        public ICommand ImportMaterialsCommand { get; }
        public ICommand ExportMaterialsCommand { get; }

        public ICommand AddPropertyCommand { get; }
        public ICommand DeletePropertyCommand { get; }

        public ICommand AssignToTankCommand { get; }
        public ICommand ChangeTankMaterialCommand { get; }
        public ICommand LoadTankHistoryCommand { get; }

        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion

        #region 编辑状态

        private MaterialCategory? _editingCategory;
        private Material? _editingMaterial;
        private bool _isEditingCategory;
        private bool _isEditingMaterial;

        public MaterialCategory? EditingCategory
        {
            get => _editingCategory;
            set => SetProperty(ref _editingCategory, value);
        }

        public Material? EditingMaterial
        {
            get => _editingMaterial;
            set => SetProperty(ref _editingMaterial, value);
        }

        public bool IsEditingCategory
        {
            get => _isEditingCategory;
            set => SetProperty(ref _isEditingCategory, value);
        }

        public bool IsEditingMaterial
        {
            get => _isEditingMaterial;
            set => SetProperty(ref _isEditingMaterial, value);
        }

        #endregion

        public MaterialManagementViewModel(MaterialService materialService)
        {
            _materialService = materialService;
            _logger = LogManager.CreateModuleLogger(nameof(MaterialManagementViewModel));

            // 初始化命令
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            NewCategoryCommand = new RelayCommand(() => StartNewCategory());
            EditCategoryCommand = new RelayCommand(() => StartEditCategory(), () => SelectedCategory != null);
            DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync, () => SelectedCategory != null && !IsEditingCategory);
            SaveCategoryCommand = new AsyncRelayCommand(SaveCategoryAsync, () => IsEditingCategory);
            CancelCategoryCommand = new RelayCommand(CancelCategoryEdit);

            NewMaterialCommand = new RelayCommand(() => StartNewMaterial());
            EditMaterialCommand = new RelayCommand(() => StartEditMaterial(), () => SelectedMaterial != null);
            DeleteMaterialCommand = new AsyncRelayCommand(DeleteMaterialAsync, () => SelectedMaterial != null && !IsEditingMaterial);
            SaveMaterialCommand = new AsyncRelayCommand(SaveMaterialAsync, () => IsEditingMaterial);
            CancelMaterialCommand = new RelayCommand(CancelMaterialEdit);
            CopyMaterialCommand = new AsyncRelayCommand(CopyMaterialAsync, () => SelectedMaterial != null);
            ImportMaterialsCommand = new AsyncRelayCommand(ImportMaterialsAsync);
            ExportMaterialsCommand = new AsyncRelayCommand(ExportMaterialsAsync);

            AddPropertyCommand = new RelayCommand(AddProperty);
            DeletePropertyCommand = new RelayCommand<MaterialProperty>(DeleteProperty);

            AssignToTankCommand = new AsyncRelayCommand(AssignToTankAsync);
            ChangeTankMaterialCommand = new AsyncRelayCommand(ChangeTankMaterialAsync);
            LoadTankHistoryCommand = new AsyncRelayCommand(LoadTankHistoryAsync);

            PrevPageCommand = new RelayCommand(() => MaterialPage--, () => MaterialPage > 1);
            NextPageCommand = new RelayCommand(() => MaterialPage++, () => MaterialPage < MaterialTotalPages);
            FirstPageCommand = new RelayCommand(() => MaterialPage = 1, () => MaterialPage > 1);
            LastPageCommand = new RelayCommand(() => MaterialPage = MaterialTotalPages, () => MaterialPage < MaterialTotalPages);

            // 自动加载
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载分类...";

                var categories = await _materialService.GetAllCategoriesAsync();
                var filtered = string.IsNullOrWhiteSpace(CategorySearchText)
                    ? categories
                    : categories.Where(c => c.Code.Contains(CategorySearchText, StringComparison.OrdinalIgnoreCase) ||
                                           c.Name.Contains(CategorySearchText, StringComparison.OrdinalIgnoreCase));

                _categories.Clear();
                foreach (var c in filtered.OrderBy(c => c.SortOrder).ThenBy(c => c.Code))
                    _categories.Add(c);

                StatusMessage = $"分类: {_categories.Count} 项";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载物料分类失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMaterialsAsync()
        {
            if (SelectedCategory == null && !_categories.Any()) return;

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载物料...";

                var filter = new MaterialFilter
                {
                    Keyword = MaterialSearchText,
                    CategoryId = SelectedCategory?.Id,
                    MaterialType = MaterialTypeFilter,
                    Status = MaterialStatusFilter
                };

                var (items, total) = await _materialService.GetMaterialsPagedAsync(MaterialPage, MaterialPageSize, filter);
                _materials.Clear();
                foreach (var m in items) _materials.Add(m);
                MaterialTotalCount = total;

                OnPropertyChanged(nameof(MaterialTotalPages));

                StatusMessage = $"物料: {_materials.Count} / {total} 项";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载物料失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnCategorySelected()
        {
            _materialPage = 1;
            await LoadMaterialsAsync();
        }

        private async void OnMaterialSelected()
        {
            if (SelectedMaterial != null)
            {
                await LoadMaterialPropertiesAsync(SelectedMaterial.Id);
                await LoadTankMaterialsAsync(SelectedMaterial.Id);
            }
            else
            {
                _materialProperties.Clear();
                _tankMaterials.Clear();
            }
        }

        private async Task LoadMaterialPropertiesAsync(int materialId)
        {
            try
            {
                var properties = await _materialService.GetMaterialPropertiesAsync(materialId);
                _materialProperties.Clear();
                foreach (var p in properties) _materialProperties.Add(p);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载物料属性失败");
            }
        }

        private async Task LoadTankMaterialsAsync(int materialId)
        {
            try
            {
                var tanks = await _materialService.GetMaterialTanksAsync(materialId);
                _tankMaterials.Clear();
                foreach (var t in tanks) _tankMaterials.Add(t);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载罐体分配失败");
            }
        }

        private async Task LoadTankHistoryAsync()
        {
            if (SelectedTankMaterial == null) return;

            try
            {
                var history = await _materialService.GetTankMaterialHistoryAsync(SelectedTankMaterial.TankId);
                _tankMaterialHistory.Clear();
                foreach (var h in history) _tankMaterialHistory.Add(h);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载罐体历史失败");
            }
        }

        #region 分类编辑

        private void StartNewCategory()
        {
            EditingCategory = new MaterialCategory
            {
                IsActive = true,
                SortOrder = _categories.Count > 0 ? _categories.Max(c => c.SortOrder) + 1 : 0
            };
            IsEditingCategory = true;
            SelectedCategory = null;
        }

        private void StartEditCategory()
        {
            if (SelectedCategory == null) return;
            EditingCategory = new MaterialCategory
            {
                Id = SelectedCategory.Id,
                Code = SelectedCategory.Code,
                Name = SelectedCategory.Name,
                Description = SelectedCategory.Description,
                SortOrder = SelectedCategory.SortOrder,
                IsActive = SelectedCategory.IsActive
            };
            IsEditingCategory = true;
        }

        private void CancelCategoryEdit()
        {
            EditingCategory = null;
            IsEditingCategory = false;
        }

        private async Task SaveCategoryAsync()
        {
            if (EditingCategory == null) return;

            try
            {
                if (EditingCategory.Id == 0)
                {
                    await _materialService.CreateCategoryAsync(EditingCategory, "CurrentUser");
                }
                else
                {
                    await _materialService.UpdateCategoryAsync(EditingCategory, "CurrentUser");
                }

                CancelCategoryEdit();
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存分类失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null) return;

            var result = MessageBox.Show(
                $"确定要删除分类 '{SelectedCategory.Name}' 吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _materialService.DeleteCategoryAsync(SelectedCategory.Id, "CurrentUser");
                    await LoadCategoriesAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "删除分类失败");
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 物料编辑

        private void StartNewMaterial()
        {
            EditingMaterial = new Material
            {
                CategoryId = SelectedCategory?.Id ?? 0,
                Status = MaterialStatus.Active,
                MaterialType = MaterialType.RawMaterial,
                Unit = "KG",
                IsWeighable = true,
                IsBatchManaged = true
            };
            IsEditingMaterial = true;
            SelectedMaterial = null;
            _materialProperties.Clear();
        }

        private void StartEditMaterial()
        {
            if (SelectedMaterial == null) return;
            EditingMaterial = CloneMaterial(SelectedMaterial);
            IsEditingMaterial = true;
        }

        private void CancelMaterialEdit()
        {
            EditingMaterial = null;
            IsEditingMaterial = false;
        }

        private async Task SaveMaterialAsync()
        {
            if (EditingMaterial == null) return;

            try
            {
                if (EditingMaterial.Id == 0)
                {
                    await _materialService.CreateMaterialAsync(EditingMaterial, "CurrentUser");
                }
                else
                {
                    await _materialService.UpdateMaterialAsync(EditingMaterial, "CurrentUser");
                }

                CancelMaterialEdit();
                await LoadMaterialsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存物料失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteMaterialAsync()
        {
            if (SelectedMaterial == null) return;

            var result = MessageBox.Show(
                $"确定要删除物料 '{SelectedMaterial.Name}' 吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _materialService.DeleteMaterialAsync(SelectedMaterial.Id, "CurrentUser");
                    await LoadMaterialsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "删除物料失败");
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task CopyMaterialAsync()
        {
            if (SelectedMaterial == null) return;

            try
            {
                var copy = CloneMaterial(SelectedMaterial);
                copy.Code = $"{SelectedMaterial.Code}_COPY";
                copy.Name = $"{SelectedMaterial.Name} (副本)";
                copy.Id = 0;

                await _materialService.CreateMaterialAsync(copy, "CurrentUser");
                await LoadMaterialsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "复制物料失败");
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Material CloneMaterial(Material source)
        {
            return new Material
            {
                Id = source.Id,
                Code = source.Code,
                Name = source.Name,
                Specification = source.Specification,
                Unit = source.Unit,
                MaterialType = source.MaterialType,
                Status = source.Status,
                CategoryId = source.CategoryId,
                Density = source.Density,
                MoistureContent = source.MoistureContent,
                ParticleSize = source.ParticleSize,
                Color = source.Color,
                Manufacturer = source.Manufacturer,
                Supplier = source.Supplier,
                StandardCost = source.StandardCost,
                Remark = source.Remark,
                IsWeighable = source.IsWeighable,
                IsBatchManaged = source.IsBatchManaged,
                ShelfLifeDays = source.ShelfLifeDays,
                StorageCondition = source.StorageCondition
            };
        }

        private async Task ImportMaterialsAsync()
        {
            // TODO: 实现Excel导入
            MessageBox.Show("导入功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportMaterialsAsync()
        {
            // TODO: 实现Excel导出
            MessageBox.Show("导出功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 物料属性

        private void AddProperty()
        {
            if (SelectedMaterial == null) return;
            _materialProperties.Add(new MaterialProperty
            {
                MaterialId = SelectedMaterial.Id,
                DataType = "String"
            });
        }

        private void DeleteProperty(MaterialProperty? property)
        {
            if (property == null) return;
            _materialProperties.Remove(property);
        }

        #endregion

        #region 罐体分配

        private async Task AssignToTankAsync()
        {
            if (SelectedMaterial == null) return;
            // TODO: 打开罐体选择对话框
            MessageBox.Show("罐体分配功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ChangeTankMaterialAsync()
        {
            if (SelectedTankMaterial == null) return;
            // TODO: 打开物料选择对话框
            MessageBox.Show("变更罐体物料功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    /// <summary>
    /// 异步命令
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                try
                {
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 同步命令
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 带参数的同步命令
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return parameter is T t && (_canExecute?.Invoke(t) ?? true);
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                _execute((T)parameter!);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}