using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ProkisoMarker
{
	public class ProblemSet : BindableBase
	{
		public ObservableCollection<Problem> Problems { get; } = new ObservableCollection<Problem>();
		private int _numBasicProblems = 0;
		public int NumBasicProblems
		{
			get { return _numBasicProblems; }
			private set { SetProperty(ref _numBasicProblems, value); }
		}
		private int _numAdvancedProblems = 0;
		public int NumAdvancedPRoblems
		{
			get { return _numAdvancedProblems; }
			private set { SetProperty(ref _numAdvancedProblems, value); }
		}

		public ProblemSet()
		{
			Problems.CollectionChanged +=Problems_CollectionChanged;
		}

		private void Problems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				foreach (var item in e.NewItems.Cast<Problem>()) {
					item.PropertyChanged += Problem_PropertyChanged;
				}
			}
			if (e.OldItems != null) {
				foreach (var item in e.OldItems.Cast<Problem>()) {
					item.PropertyChanged -= Problem_PropertyChanged;
				}
			}
			UpdateNumProblems();
		}

		private void Problem_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var p = (Problem)sender;
			if(e.PropertyName == nameof(Problem.Type)) {
				UpdateNumProblems();
			}
		}

		private void UpdateNumProblems()
		{
			int b = 0, a = 0;
			foreach (var item in Problems) {
				switch (item.Type) {
				case ProblemType.Basic:
					++b;
					break;
				case ProblemType.Advanced:
					++a;
					break;
				}
			}
			NumBasicProblems = b;
			NumAdvancedPRoblems = a;
		}
	}
}
