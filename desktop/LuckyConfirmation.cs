using System.Windows;
using System.Windows.Controls;
namespace BD2ApostleDefense.Desktop;
internal sealed class LuckyConfirmation:Window
{
 public LuckyConfirmation()
 {
  Title=Ui.Text("开启好运模式");Width=410;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;
  WindowStartupLocation=WindowStartupLocation.CenterOwner;ShowInTaskbar=false;
  Background=(System.Windows.Media.Brush)Application.Current.FindResource("AppBackground");
  var panel=new StackPanel{Margin=new Thickness(24)};
  panel.Children.Add(new TextBlock{Text=Ui.Text("此功能可能存在风险，请自负风险。是否开启？"),TextWrapping=TextWrapping.Wrap,FontSize=15,LineHeight=24,Margin=new Thickness(0,0,0,24)});
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  var cancel=new Button{Content=Ui.Text("取消"),IsCancel=true,IsDefault=true,MinWidth=88,Margin=new Thickness(0,0,8,0)};
  var enable=new Button{Content=Ui.Text("确认开启"),MinWidth=88};
  cancel.Click+=(_,_)=>{DialogResult=false;};enable.Click+=(_,_)=>{DialogResult=true;};
  buttons.Children.Add(cancel);buttons.Children.Add(enable);panel.Children.Add(buttons);Content=panel;
 }
}
