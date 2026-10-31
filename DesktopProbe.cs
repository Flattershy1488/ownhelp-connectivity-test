using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class DesktopProbe {
 [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public MOUSEINPUT mi; }
 [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx,dy; public uint mouseData,dwFlags,time; public UIntPtr extra; }
 [DllImport("user32.dll")] static extern uint SendInput(uint count,INPUT[] values,int size);
 [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
 [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr desktop);
 [STAThread] static int Main() {
  try {
   SetProcessDPIAware();
   if(!Environment.UserInteractive || Process.GetCurrentProcess().SessionId==0) { Console.WriteLine("PROBE_BLOCKED_NO_INTERACTIVE_SESSION"); return 2; }
   var desktop=OpenInputDesktop(0,false,0x0100);
   if(desktop==IntPtr.Zero) { Console.WriteLine("PROBE_BLOCKED_NO_INPUT_DESKTOP"); return 3; }
   CloseDesktop(desktop); Application.EnableVisualStyles();
   int result=4,clicks=0,stage=0; var elapsed=Stopwatch.StartNew();
   using(var form=new Form {Text="Disposable desktop test",BackColor=Color.Lime,Size=new Size(640,480),StartPosition=FormStartPosition.CenterScreen,TopMost=true})
   using(var timer=new Timer {Interval=150}) {
    var button=new Button {Text="Test input",Bounds=new Rectangle(160,160,240,80)};
    button.Click+=delegate {clicks++;}; form.Controls.Add(button);
    timer.Tick+=delegate {
     try {
      if(elapsed.Elapsed.TotalSeconds>12) {Console.WriteLine("PROBE_FAILED_INPUT_TIMEOUT");form.Close();return;}
      if(stage==0) {
       form.Activate(); Point p=form.PointToScreen(new Point(30,60));
       using(var bitmap=new Bitmap(1,1,PixelFormat.Format24bppRgb)) {
        using(var g=Graphics.FromImage(bitmap)) g.CopyFromScreen(p,Point.Empty,new Size(1,1));
        var color=bitmap.GetPixel(0,0); if(color.G<200 || color.R>70 || color.B>70) return;
       }
       Console.WriteLine("PROBE_REAL_SCREEN_CAPTURE_OK");
       var center=button.PointToScreen(new Point(button.Width/2,button.Height/2));
       if(!SetCursorPos(center.X,center.Y)) throw new InvalidOperationException();
       var input=new[]{new INPUT{mi=new MOUSEINPUT{dwFlags=0x0002}},new INPUT{mi=new MOUSEINPUT{dwFlags=0x0004}}};
       if(SendInput(2,input,Marshal.SizeOf(typeof(INPUT)))!=2) throw new InvalidOperationException();
       stage=1;
      } else if(clicks==1) {
       Console.WriteLine("PROBE_NATIVE_MOUSE_CLICK_OK"); Console.WriteLine("PROBE_PASS_ENVIRONMENT_ONLY_NOT_OWNHELP_TEST"); result=0; form.Close();
      }
     } catch {Console.WriteLine("PROBE_FAILED_DESKTOP_OPERATION");form.Close();}
    };
    form.Shown+=delegate {elapsed.Restart();timer.Start();}; Application.Run(form); timer.Stop();
   }
   return result;
  } catch {Console.WriteLine("PROBE_FAILED_INITIALIZATION");return 5;}
 }
}
