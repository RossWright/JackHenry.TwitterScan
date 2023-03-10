# Twitter Scan Interview Project

## Solution Structure
All projects are prefixed with Client.Project (JackHenry.TwitterScan)
- **Common** - a class library with definitions common across projects
- **Emitter** - an ASP.NET Web API project that streams mock tweets. The URL is https://localhost:7260/stream?rate=57 
	where rate is the number of tweets per second to output to the stream. See performance notes below
- **PocConsole** - an interactive console app that connects to either the twitter stream or emitter and displays live statistics on feed volume and rate.
- **Service** - an ASP.NET Web API project that consumes a tweet stream (twitter or emitter) in a background service
	and exposes an API to get statistics for elapsed time, tweet count and the top 10 hashtags (and their count of occurrence)
- **Service.Tests** - a class library of XUnit Unit tests for the services in the Service project (with 100% coverage, though I could think of more esoteric tests)
- **Web** - a Blazor WASM web project that polls the Service API and displays the statistics in real-time
- **RossWright.Tools** - A set of tools imported (and improved) from my personal reuse library
	
## How to Run the solution

### To execute this project using the Emitter test stream:
The solution is committed to the repository ready to run against the emitter. You just need to setup the startup projects and run:

1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.Service project, edit the appsettings.json file
3. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
4. Select the Multiple startup projects option
5. Change the Action next to the following projects to "Start": JackHenry.TwitterScan.Emitter, JackHenry.TwitterScan.Service and JackHenry.TwitterScan.Web
6. Build and Run. In addition to the three consoles, two browser windows will open: 
		one with the Swagger page for the API and another with 
		a Blazor WASM app that polls the API every 500ms and displays the results 

Note to adjust the throttling rate for the emitter, edit the rate query parameter of the TwitterConnection:Url in JackHenry.TwitterScan.Service/appsettings.json

### To execute the project using the live Twitter stream:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.Service project, edit the appsettings.json file
3. Set TwitterConnection:AccessToken to your valid Twitter Bearer token (comment out the token for the Emitter) and
	Ensure the TwitterConnection:Url line with twitter.com is uncommented and the one with localhost is commented out
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. Change the Action next to the following projects to "Start": JackHenry.TwitterScan.Service and JackHenry.TwitterScan.Web
7. Build and Run. In addition to the three consoles, two browser windows will open: one with the Swagger page for the API and another with a Blazor WASM app that polls the API every 500ms and displays the results 

### To execute the POC Console:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.PocConsole project, edit the appsettings.json file
3. Set TwitterApi:Jwt to your valid Twitter Bearer token (if you plan to run against Twitter's API)
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. If you plan to use the mock emitter, change the Action next to the following projects to "Start": JackHenry.TwitterScan.Emitter and JackHenry.TwitterScan.PocConsole. Otherwise, just set PocConsole as the single project to start
7. Build and Run. The console window will open, prompt you to choose live twitter feed (default) or set an Emitter rate. It will then display 2 lines: a header line and value line with the elapsed time in seconds, tweet count and rate of tweets per second received which will update in place in real time.
	
## Notes

### Recent changes
Note that I completed an initial pass of the project even though I was unable to get a Twitter API bearer token. Before I knew that was an issue the dev plan I submitted included a "Mock emitter", so I was able to complete the project without Twitter access. Though it sounds like I may never know if my emitter truly simulates the exact behavior of the Twitter API. I built it using the API docs as best I could follow - but in the real world it seems there is always some little difference. Initially, I focused my efforts on performance and made some architectural compromises in an effort to submit quickly. I was informed on Thursday that the twitter API issue got even worse for all candidates and that my solution would be reviewed next week, so I took the opportunity to revise the project to focus more on architectural concepts. 

My new goal was to make the server completely OCP compliant, so I brought in some tools from my personal toolkit (code included) that use reflection for service registration and refactored my code for the possibility of multiple tweet processors. As an exercise, I was able to add a Metric Processor (Impressions, Likes, etc.) by only adding classes to the server (and test project) without modifying a single line of existing code on the server or test projects. Granted, I did have to modify the emitter code to include the relevant test data and the web monitor code to access the new endpoints - but as those are just test tools for the server and not the focus of the exercise, it seemed forgivable. If the web site was part of the release code, I would have employed a similar architecture there.

### Performance
The speed of the emitter is a function of the site of the tweet structures returned. All measurements are rounded to the most significant digit and were measured using my POCConsole running on the server (on my battlestation) for a few minutes. My initial runs of the emitters ran at 1.5M t/s (tweets per second). Adding a few random hashtags to each tweet slowed that down to 600K t/s. Adding metrics has reduced that to 500K t/s. The streamlined OCP-nightmare hashtag-only iteration of the server was able to keep up with 500K t/s, but fixing the architecture for OCP expansion brought that down to 400K and adding the metrics process and splitting out the count processor brings it down to about 300K t/s. 

I have the server configured to 300K t/s in the committed appsettings.config for the server project. It is important to throttle the emitter close to the capacity of the server to avoid a build-up of the buffer. And of course, everything runs just fine at realistic rates like 57 t/s or 5700 t/s.

### Scaling
Given that this implementation seems to handle far more tweets per second - albeit locally on my machine - than the expected 5,700 tweets per second, I think there isn't really a future scaling improvement for the code itself that could be solved without using some infrastructure tricks for this particular toy problem. 

But, assuming for a moment there were scaling challenges on the horizon, I don't see any way in-process to make this drastically more efficient. My solution to scale further would go something like this: 
1. I would move to a parallel architecture with multiple Tweet Stream Reader servers on multiple machines feeding a single Tweet Statistics Repository server.
2. I would need to partition the queries for the streams in some way to ensure the Tweet Stream Reader servers were not receiving duplicate tweets. Likely segregating tweets by some property, query and/or running only a limited number of Tweet Processors on each server.
3. Each Tweet Stream Reader server would aggregate count and count per hashtag locally for it's stream, as the current solution does, and periodically send an update of those stats (all hashtags encountered, not just the top ten) to the Tweet Statistics Repository server that would aggregate those updates to its own accounting - which the API would report from for aggregated stats.
4. The API to access the stats would sit on the Tweet Statistics Repository server. If we needed to scale the API access for massive numbers of queries, a web farm where each Tweet Statistics Repository server is on the backbone maintaining its own aggregate stats would scale that end. Since the frequency of statistics change is slower, caching could also be utilized in the infrastructure level to just return the last response to all callers with a cache invalidation every 500ms.
