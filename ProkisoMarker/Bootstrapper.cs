using ProkisoMarker.Views;
using System.Windows;
using Prism.Modularity;
using Autofac;
using Prism.Autofac;
using ProkisoMarker.Models;

namespace ProkisoMarker
{
	class Bootstrapper : AutofacBootstrapper
	{
		protected override DependencyObject CreateShell()
		{
			return Container.Resolve<MainWindow>();
		}

		protected override void InitializeShell()
		{
			Application.Current.MainWindow.Show();
		}

		protected override void ConfigureModuleCatalog()
		{
			var moduleCatalog = (ModuleCatalog)ModuleCatalog;
			//moduleCatalog.AddModule(typeof(YOUR_MODULE));
		}

		protected override void ConfigureContainerBuilder(ContainerBuilder builder)
		{
			base.ConfigureContainerBuilder(builder);
			builder.RegisterType<Model>().As<IModel>();
		}
	}
}
