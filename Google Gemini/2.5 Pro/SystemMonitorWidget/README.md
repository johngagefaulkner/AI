# System Monitor Widget

I set out with the intentions of learning how to create "headless" WinUI 3 app windows (likely using `AppWindowPresenter` as far as I could tell) to try to mimic some of the more recent WinUI 3 / WindowsAppSDK projects I've seen starting to pop-up on GitHub. I settled on the idea of a CPU hardware utilization app which was perfect considering I'd also been looking for a reason to try out the latest "WinUI 3-friendly" version of LiveCharts2.

## Prompt

> I want to build an app using WinUI 3, C# 14.0, .NET 10, and the Windows App SDK that uses AppWindowPresenter to show a headless window containing the graphs/charts/gauges displaying the user’s CPU, RAM, GPU, and Storage utilization. See the attached image for design reference and concept context. 

### Reference Image (attached to prompt)

![unnamed](https://github.com/user-attachments/assets/6863c6d0-dadf-401c-8bca-5a70f7022e85)

# Result

Now, I haven't quite figured out yet why Gemini 2.5 Pro decided to go this way (take this route, use this methodology, whatever) but the response I received was a Console app that loads all the XAML for the UI from a string... interesting. Take a look!
