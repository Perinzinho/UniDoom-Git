Add-Type -TypeDefinition @'
using System;using System.Text;using System.Runtime.InteropServices;
public static class CinemaRefresh {
 [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr h,int i);
 [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr h,uint i,StringBuilder s,int n,uint f);
 [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr h,int i);
 [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr h,uint m,UIntPtr w,IntPtr l,uint f,uint t,out UIntPtr r);
 public static uint Find(IntPtr m){for(int i=0;i<GetMenuItemCount(m);i++){var s=new StringBuilder(512);GetMenuString(m,(uint)i,s,512,0x400);if(s.ToString().Replace("&","").Split('\t')[0].Trim()=="Refresh")return GetMenuItemID(m,i);var sub=GetSubMenu(m,i);if(sub!=IntPtr.Zero){uint v=Find(sub);if(v!=0)return v;}}return 0;}
}
'@
$cinemaEditor=Get-Process -Id 1436 -ErrorAction Stop
$cinemaCommand=[CinemaRefresh]::Find([CinemaRefresh]::GetMenu($cinemaEditor.MainWindowHandle))
if($cinemaCommand -eq 0){throw 'Existing Assets/Refresh unavailable; no focus/activation attempted.'}
$cinemaResult=[UIntPtr]::Zero
[void][CinemaRefresh]::SendMessageTimeout($cinemaEditor.MainWindowHandle,0x111,[UIntPtr]$cinemaCommand,[IntPtr]::Zero,2,1000,[ref]$cinemaResult)
Write-Output 'Requested Assets/Refresh on existing editor, without window opening, focus or game.'
