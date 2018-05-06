using Prism.Mvvm;
using ProkisoMarker.Models;

namespace ProkisoMarker.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		public IModel Model { get; }

		private string _title = "ProkisoMarker";
		public string Title
		{
			get { return _title; }
			set { SetProperty(ref _title, value); }
		}

		public MainWindowViewModel(IModel model)
		{
			Model = model;
		}
	}
}
