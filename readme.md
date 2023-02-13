# Twitter Scan InterView Project

##To execute the project using the twitter stream:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.Service project, edit the appsettings.json file
3. Set TwitterConnection:AccessToken to your valid Twitter Bearer token and
	Ensure the TwitterConnection:Url line with twitter.com is uncommented and the one with localhost is commented
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. Change the Action next to the following projects to "Start":
		JackHenry.TwitterScan.Service
		JackHenry.TwitterScan.Web
7. Build and Run. In addition to the three consoles, two browser windows will open: 
		one with the Swagger page for the API and another with 
		a Blazor WASM app that polls the API every 500ms and displays the results 
	
##To execute this project using the test twitter emitter:
As above, except:
	On Step 3, Comment out the TwitterConnection:Url line with twitter.com and uncomment one with localhost
		Note that the AccessToken is ignored, so if you already added it, you can leave it. 
	On step 6, also set the JackHenry.TwitterScan.Emitter project to Start
	Note that the throttling is set to 1M tweets per second (effectively unthrottled) on the emitter. 
	To adjust that, edit the rate query parameter of the TwitterConnection:Url in JackHenry.TwitterScan.Service/appsettings.json

##To execute the POC Console:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.PocConsole project, edit the appsettings.json file
3. Set TwitterAccessToken to your valid Twitter Bearer token
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. If you plan to use the mock emitter, change the Action next to the following projects to "Start":
		JackHenry.TwitterScan.Emitter
		JackHenry.TwitterScan.PocConsole
	Otherwise, just set PocConsole as the single project to start
7. Build and Run. The console window will open and just display 2 lines, a header and values for the elapsed time, tweet count and rate which will update in place in real time.
	
##Solution Structure
All projects are prefixed with Client.Project (JackHenry.TwitterScan)
- **Common** - a class library with definitions common across projects
- **Emitter** - an ASP.NET Web API project that streams mock tweets. The url is https://localhost:7260/stream?rate=57 
	where rate is the number of tweets per second to stream. On my machine is caps out around 720k/s
- **PocConsole** - an interactive console app that connects to either the twitter stream or emitter and displays feed volume and rate live.
- **Serivce** - an ASP.NET Web API project that consumes a tweet stream (twitter or emitter) in a background service
	and exposes an API to get statistics for elapsed time, tweet count and the top 10 hashtags (and their count of occurance)
- **Service.Tests** - a class library of XUnit Unit tests for the services in the Service project
- **Web** - a Blazor WASM web project that polls the Service API and displays the statistics in real-time

##Notes

###Quality 
The Common, Service, ServiceTests and Web projects are meant to be up to production quality. 
The Emitter and PocConsole projects are more-or-less "test scripts", but I tried to keep it clean.

###Balance of Illustration vs Pragmatism
I struggled with finding balance between consise organization verses illustrating concepts for less trivial systems. In the end, I hope I found a balance that is acceptable. 
One area I stopped short of was API Versioning. It's critical to consider this when developing RESTful APIs from the begining. 
I theorectically could have broken the classes down to absurdity, but what I have is representative of the choices I would make in a real system. 
I felt trying to turn this little project into a SOLID/Design Pattern tour de force, would make me look like an Architecture Astronaut. I'm actally very pragmatic, so I just applied what techniques seemed appropriate to the scope.

###Scaling
I approached the problem agnostic of the load the stream would encounter - I built to handle a stream at the limits of the hardware where the solution is deployed. My mock emitter blasts out 720,000 tweets a second and the solution handles it without issue. Given that the expectation was that Twitter as a whole only generates 5700 tweets per second, I think there isn't really a future scaling issue that cannot be solved using infrastructure for this particular toy problem. 
But assuming for a moment there was a scaling challenge on the horizon. I don't see a way in-process to make this more efficient. My solution to scale further would go something like this: 
1. I would move to a parallel architecture with multiple TweetReceiver servers on multiple machines feeding a single TweetStat server.
2. I would need to partition the queries for the streams in some way to ensure multiple TweetReceiver servers were not receiving duplicate tweets. 
3. Each TweetReceiver server would aggregate count and count per hashtag locally for it's stream query, as the current solution does, and periodically send an update of those stats (all hashtags encountered, not just the top ten) to the TweetStat server that would aggregate those applies those updates to it's own accounting - which the API would report from for aggregated stats.
4. The API to access the stats would sit on the TweetStat server. If we needed to scale the API access, a web farm where each TweetStat server is on the backbone and maintaining it;s own aggregate stats would scale that end.




