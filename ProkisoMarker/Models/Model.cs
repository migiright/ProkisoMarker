using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProkisoMarker.Models
{
	public class Model : BindableBase, IModel
	{
		public ProblemSet ProblemSet { get; } = new ProblemSet();

		public Model()
		{
			ProblemSet.Problems.CollectionChanged += Problems_CollectionChanged;
		}

		private void Problems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				foreach (var (p, i) in e.NewItems.Cast<Problem>().Select((p, i) => (p, i))) {
					if (string.IsNullOrEmpty(p.Name)) {
						p.Name = "p" + (i + 1 + ProblemSet.Problems.Count - e.NewItems.Count);
					}
				}
			}
			foreach (var (p, i) in ProblemSet.Problems.Select((p, i) => (p, i))) {
				p.No = i + 1;
			}
		}
	}
}
