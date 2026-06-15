public interface IView<in TViewModel> where TViewModel : IViewModel
{
    void Bind(TViewModel viewModel);
    void Unbind();
}
