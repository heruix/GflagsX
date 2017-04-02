﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GflagsX.Models;
using GflagsX.Views;
using Microsoft.Win32;
using Prism.Commands;

namespace GflagsX.ViewModels {
	class ImageTabViewModel : GlobalFlagsTabViewModelBase {
		const string IFEOKey = @"Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

		public string Text => "Image";

		public string Icon => "/icons/image.ico";

		ObservableCollection<string> _images;

		public IList<string> Images => _images;

		public ImageTabViewModel() : base(GlobalFlagUsage.Image) {
			RefreshAll();
		}

		private string _selectedImage;

		public string SelectedImage {
			get { return _selectedImage; }
			set {
				if(SetProperty(ref _selectedImage, value)) {
					CalculateFlags();
					OnPropertyChanged(nameof(Flags));
				}
			}
		}

		public override void Apply() {
			if(SelectedImage == null)
				return;

			using(var key = Registry.LocalMachine.OpenSubKey(IFEOKey + "\\" + SelectedImage, true)) {
				var value = FlagsValue;
				key.SetValue("GlobalFlag", value, RegistryValueKind.DWord);
			}
		}

		protected override void CalculateFlags() {
			if(SelectedImage == null)
				return;

			using(var key = Registry.LocalMachine.OpenSubKey(IFEOKey + "\\" + SelectedImage)) {
				var value = key.GetValue("GlobalFlag");
				var ntGlobalFlags = value == null ? 0 : (int)value;
				foreach(var vm in Flags) {
					vm.IsEnabled = (ntGlobalFlags & vm.Flag.Value) == vm.Flag.Value;
				}
			}
		}

		public ICommand NewImageCommand => new DelegateCommand(() => {
		var vm = App.MainViewModel.UI.DialogService.CreateDialog<NewImageViewModel, NewImageView>();
		if(vm.ShowDialog() == true) {
				if(Images.Contains(vm.ImageName, StringComparer.InvariantCultureIgnoreCase)) {
					App.MainViewModel.UI.MessageBoxService.ShowMessage("Image name already exists.", Constants.AppName);
				}
				else {
					// add to registry and the list
					using(var key = Registry.LocalMachine.OpenSubKey(IFEOKey, true)) {
						key.CreateSubKey(vm.ImageName);
					}
					var list = _images.ToList();
					list.Add(vm.ImageName);
					_images.Insert(list.IndexOf(vm.ImageName), vm.ImageName);
					SelectedImage = vm.ImageName;
				}
			}
		});

		public ICommand DeleteImageCommand => new DelegateCommand(() => {
			using(var key = Registry.LocalMachine.OpenSubKey(IFEOKey, true)) {
				key.DeleteSubKey(SelectedImage);
			}
			int index = _images.IndexOf(SelectedImage);
			Debug.Assert(index >= 0);
			_images.RemoveAt(index);
			if(_images.Count > 0)
				SelectedImage = _images[index];
		}, () => SelectedImage != null).ObservesProperty(() => SelectedImage);

		public ICommand RefreshAllCommand => new DelegateCommand(() => RefreshAll());

		private void RefreshAll() {
			using(var key = Registry.LocalMachine.OpenSubKey(IFEOKey)) {
				_images = new ObservableCollection<string>(key.GetSubKeyNames());
				if(_images.Count > 0)
					SelectedImage = _images[0];
				OnPropertyChanged(nameof(Images));
			}
		}
	}
}