using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using ScreenshotInterface;
using EasyHook;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.IO;

namespace TestScreenshot
{
    public partial class Form1 : Form
    {
        private String ChannelName = null;
        private IpcServerChannel ScreenshotServer;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialise the IPC server
            ScreenshotServer = RemoteHooking.IpcCreateServer<ScreenshotInterface.ScreenshotInterface>(
                ref ChannelName,
                WellKnownObjectMode.Singleton);

            ScreenshotManager.OnScreenshotDebugMessage += new ScreenshotDebugMessage(ScreenshotManager_OnScreenshotDebugMessage);
        }

        private void btnInject_Click(object sender, EventArgs e)
        {
            btnInject.Enabled = false;

            if (cbAutoGAC.Checked)
            {
                // NOTE: On some 64-bit setups this doesn't work so well.
                //       Sometimes if using a 32-bit target, it will not find the GAC assembly
                //       without a machine restart, so requires manual insertion into the GAC
                // Alternatively if the required assemblies are in the target applications
                // search path they will load correctly.

                // Must be running as Administrator to allow dynamic registration in GAC
                Config.Register("ScreenshotInjector",
                    "ScreenshotInject.dll");
            }

            AttachProcess();
        }

        int processId = 0;
        Process _process;
        private void AttachProcess()
        {
            bool newInstanceFound = false;

            Direct3DVersion direct3DVersion = Direct3DVersion.Direct3D10;

            if (rbDirect3D11.Checked)
            {
                direct3DVersion = Direct3DVersion.Direct3D11;
            }
            else if (rbDirect3D10_1.Checked)
            {
                direct3DVersion = Direct3DVersion.Direct3D10_1;
            }
            else if (rbDirect3D10.Checked)
            {
                direct3DVersion = Direct3DVersion.Direct3D10;
            }
            else if (rbDirect3D9.Checked)
            {
                direct3DVersion = Direct3DVersion.Direct3D9;
            }
            else if (rbAutodetect.Checked)
            {
                direct3DVersion = Direct3DVersion.AutoDetect;
            }

            string exeName = Path.GetFileNameWithoutExtension(textBox1.Text); 
            while (!newInstanceFound)
            {
                Process[] processes = Process.GetProcessesByName(exeName);
                foreach (Process process in processes)
                {
                    // Simply attach to the first one found.

                    // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    // Skip if the process is already hooked (and we want to hook multiple applications)
                    if (HookManager.IsHooked(process.Id))
                    {
                        continue;
                    }

                    // Keep track of hooked processes in case more than one needs to be hooked
                    HookManager.AddHookedProcess(process.Id);
                    
                    processId = process.Id;
                    _process = process;

                    // Inject DLL into target process
                    RemoteHooking.Inject(
                        process.Id,
                        InjectionOptions.Default,
                        typeof(ScreenshotInject.ScreenshotInjection).Assembly.Location,//"ScreenshotInject.dll", // 32-bit version (the same because AnyCPU) could use different assembly that links to 32-bit C++ helper dll
                        typeof(ScreenshotInject.ScreenshotInjection).Assembly.Location, //"ScreenshotInject.dll", // 64-bit version (the same because AnyCPU) could use different assembly that links to 64-bit C++ helper dll
                    // the optional parameter list...
                        ChannelName, // The name of the IPC channel for the injected assembly to connect to
                        direct3DVersion.ToString(), // The direct3DVersion used in the target application
                        cbDrawOverlay.Checked
                    );

                    // Ensure the target process is in the foreground,
                    // this prevents an issue where the target app appears to be in 
                    // the foreground but does not receive any user inputs.
                    // Note: the first Alt+Tab out of the target application after injection
                    //       may still be an issue - switching between windowed and 
                    //       fullscreen fixes the issue however (see ScreenshotInjection.cs for another option)
                    BringProcessWindowToFront(process);

                    newInstanceFound = true;
                    break;
                }
                Thread.Sleep(10);
            }

            btnLoadTest.Enabled = true;
            btnCapture.Enabled = true;
        }

        /// <summary>
        /// Bring the target window to the front and wait for it to be visible
        /// </summary>
        /// <remarks>If the window does not come to the front within approx. 30 seconds an exception is raised</remarks>
        private void BringProcessWindowToFront(Process process)
        {
            if (process == null)
                return;
            IntPtr handle = process.MainWindowHandle;
            int i = 0;

            while (!NativeMethods.IsWindowInForeground(handle))
            {
                if (i == 0)
                {
                    // Initial sleep if target window is not in foreground - just to let things settle
                    Thread.Sleep(250);
                }

                if (NativeMethods.IsIconic(handle))
                {
                    // Minimized so send restore
                    NativeMethods.ShowWindow(handle, NativeMethods.WindowShowStyle.Restore);
                }
                else
                {
                    // Already Maximized or Restored so just bring to front
                    NativeMethods.SetForegroundWindow(handle);
                }
                Thread.Sleep(250);

                // Check if the target process main window is now in the foreground
                if (NativeMethods.IsWindowInForeground(handle))
                {
                    // Leave enough time for screen to redraw
                    Thread.Sleep(1000);
                    return;
                }

                // Prevent an infinite loop
                if (i > 120) // about 30secs
                {
                    throw new Exception("Could not set process window to the foreground");
                }
                i++;
            }
        }

        /// <summary>
        /// Display debug messages from the target process
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="message"></param>
        void ScreenshotManager_OnScreenshotDebugMessage(int clientPID, string message)
        {
            txtDebugLog.Invoke(new MethodInvoker(delegate()
                {
                    txtDebugLog.Text = String.Format("{0}:{1}\r\n{2}", clientPID, message, txtDebugLog.Text);
                })
            );
        }

        DateTime start;
        DateTime end;

        private void btnCapture_Click(object sender, EventArgs e)
        {
            start = DateTime.Now;
            progressBar1.Maximum = 1;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            DoRequest();
        }

        private void btnLoadTest_Click(object sender, EventArgs e)
        {
            // Note: we bring the target application into the foreground because
            //       windowed Direct3D applications have a lower framerate 
            //       if not the currently focused window
            BringProcessWindowToFront(_process);
            start = DateTime.Now;
            progressBar1.Maximum = Convert.ToInt32(txtNumber.Text);
            progressBar1.Minimum = 0;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            DoRequest();
        }

        /// <summary>
        /// Create the screen shot request
        /// </summary>
        void DoRequest()
        {
            progressBar1.Invoke(new MethodInvoker(delegate()
                {
                    if (progressBar1.Value < progressBar1.Maximum)
                    {
                        progressBar1.PerformStep();
                        // Add a request to the screenshot manager - the ScreenshotInterface will pass this on to the injected assembly
                        ScreenshotManager.AddScreenshotRequest(processId, new ScreenshotRequest(new Rectangle(int.Parse(txtCaptureX.Text), int.Parse(txtCaptureY.Text), int.Parse(txtCaptureWidth.Text), int.Parse(txtCaptureHeight.Text))), Callback);
                    }
                    else
                    {
                        end = DateTime.Now;
                        MessageBox.Show((end - start).ToString());
                    }
                })
            );
        }

        /// <summary>
        /// The callback for when the screenshot has been taken
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="status"></param>
        /// <param name="screenshotResponse"></param>
        void Callback(Int32 clientPID, ResponseStatus status, ScreenshotResponse screenshotResponse)
        {
            try
            {
                if (screenshotResponse != null && screenshotResponse.CapturedBitmap != null)
                {
                    pictureBox1.Invoke(new MethodInvoker(delegate()
                    {
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                        }
                        pictureBox1.Image = screenshotResponse.CapturedBitmapAsImage;
                    })
                    );
                }

                Thread t = new Thread(new ThreadStart(DoRequest));
                t.Start();
            }
            catch
            {
            }
        }
    }
}
