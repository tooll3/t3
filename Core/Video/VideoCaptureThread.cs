using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace T3.Core.Video;

public sealed class VideoFrameReceivedEventArgs : EventArgs
{
    private VideoFrame frame;

    public VideoFrame Frame => this.frame;

    public VideoFrameReceivedEventArgs(VideoFrame frame)
    {
        this.frame = frame;
    }
}

public abstract class VideoCaptureThread
{
    private Thread captureThread;
    private bool isRunning;
    public int ThreadSleepTime = 1;

    public EventHandler<VideoFrameReceivedEventArgs> FrameReceived;

    public void Start()
    {
        if (this.isRunning)
            return;

        this.isRunning = true;
        this.Configure();
        this.captureThread = new Thread(new ThreadStart(this.Run));
        this.captureThread.Start();
    }

    public void Stop()
    {
        if (!this.isRunning)
            return;

        this.captureThread.Join();
        this.isRunning = false;
    }

    private void Run()
    {
        while (this.isRunning)
        {
            VideoFrame? frame = this.Capture();

            if (frame.HasValue)
            {
                if (this.FrameReceived != null)
                {
                    this.FrameReceived(this, new VideoFrameReceivedEventArgs(frame.Value));
                }
            }
            else
            {
                if (this.ThreadSleepTime > 0)
                    Thread.Sleep(this.ThreadSleepTime);
            }
        }
    }

    protected abstract void Configure();
    protected abstract VideoFrame? Capture();

    public void Dispose()
    {
        this.Stop();
    }
}