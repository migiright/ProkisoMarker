using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProkisoMarker
{
	public class Student : BindableBase
	{
		private int _studentNo;
		public int StudentNo
		{
			get { return _studentNo; }
			set { SetProperty(ref _studentNo, value); }
		}
		private string _name;
		public string Name
		{
			get { return _name; }
			set { SetProperty(ref _name, value); }
		}
		public ObservableCollection<Answer> Answers { get; } = new ObservableCollection<Answer>();
		private int? _score;
		public int? Score
		{
			get { return _score; }
			private set { SetProperty(ref _score, value); }
		}
		private int? _advancedScore;
		public int? AdvancedScore
		{
			get { return _advancedScore; }
			private set { SetProperty(ref _advancedScore, value); }
		}
		private ProblemSet _problemSet;
		public ProblemSet ProblemSet
		{
			get { return _problemSet; }
			set { SetProperty(ref _problemSet, value); }
		}

		public Student(ProblemSet problemSet)
		{
			ProblemSet = problemSet;
			Answers.CollectionChanged += Answers_CollectionChanged;
			foreach (var item in ProblemSet.Problems) {
				item.PropertyChanged += Problem_PropertyChanged;
			}
			ProblemSet.Problems.CollectionChanged += Problems_CollectionChanged;
		}

		private void Answers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				foreach (var item in e.NewItems.Cast<Answer>()) {
					item.PropertyChanged += Answer_PropertyChanged;
				}
			}
			if (e.OldItems != null) {
				foreach (var item in e.OldItems.Cast<Answer>()) {
					item.PropertyChanged -= Answer_PropertyChanged;
				}
			}
			CalculateScore();
		}

		private void Answer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var a = (Answer)sender;
			if (e.PropertyName == nameof(Answer.Evaluation)) {
				CalculateScore();
			}
		}

		private void Problems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if(e.NewItems != null) {
				foreach (var item in e.NewItems.Cast<Problem>()) {
					item.PropertyChanged += Problem_PropertyChanged;
				}
			}
			if(e.OldItems != null) {
				foreach (var item in e.OldItems.Cast<Problem>()) {
					item.PropertyChanged -= Problem_PropertyChanged;
				}
			}
			CalculateScore();
		}

		private void Problem_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var p = (Problem)sender;
			if(e.PropertyName == nameof(Problem.Type)) {
				CalculateScore();
			}
		}

		private void CalculateScore()
		{
			var zip = Enumerable.Zip(Answers, ProblemSet.Problems, (a, p) => (a: a, p: p)).ToList();
			int nCorrect = zip.Where(i => i.p.Type == ProblemType.Basic).Count(), nWrong = 0,
				nAdvanced = zip.Where(i => i.p.Type == ProblemType.Advanced).Count();
			foreach (var (a, p) in Enumerable.Reverse(zip)) {
				switch (p.Type) {
				case ProblemType.Basic:
					switch (a.Evaluation) {
					case Evaluation.Undefined:
						Score = null;
						AdvancedScore = null;
						return;
					case Evaluation.NotExecutable:
						--nCorrect;
						break;
					case Evaluation.Wrong:
						--nCorrect;
						++nWrong;
						break;
					case Evaluation.Correct:
						goto calcScore;
					}
					break;
				case ProblemType.Advanced:
					switch (a.Evaluation) {
					case Evaluation.Undefined:
						Score = null;
						AdvancedScore = null;
						return;
					case Evaluation.NotExecutable:
					case Evaluation.Wrong:
						--nAdvanced;
						break;
					case Evaluation.Correct:
						goto calcScore;
					}
					break;
				}
			}
		calcScore:
			Score = (int)Math.Ceiling(60 + 40*(nCorrect + nWrong/2.0) / ProblemSet.NumBasicProblems);
			AdvancedScore = nAdvanced;
		}
	}
}
