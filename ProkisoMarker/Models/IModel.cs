
using System.Collections.ObjectModel;

namespace ProkisoMarker.Models
{
	public interface IModel
	{
		ObservableCollection<Problem> Problems { get; }
	}
}
