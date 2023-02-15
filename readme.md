# Twitter Scan Interview Project

## Important: No testing has been done with Twitter
I applied for a twitter account on Friday, Feb 10. As of this writing on Feb 14th, I have yet to receive a response. I tried setting up an alt account, with the same results. I asked around my network for an account I could use and several of my friends and colleagues tried to sign up for twitter to get me an account, but to no avail. My initial approach included building a streaming server of my own, called the Emitter, so that I could test the solution against higher stream rates than provided by Twitter. In the end, without Twitter credentials, I completed the exercise using only the emitter. I believe the project will work against a live twitter stream, but I cannot confirm it.

## Solution Structure
All projects are prefixed with Client.Project (JackHenry.TwitterScan)
- **Common** - a class library with definitions common across projects
- **Emitter** - an ASP.NET Web API project that streams mock tweets. The url is https://localhost:7260/stream?rate=57 
	where rate is the number of tweets per second to stream. On my machine it caps out around 500 Kilotweets/s, but the service caps out at 400 Kilotweets/s
- **PocConsole** - an interactive console app that connects to either the twitter stream or emitter and displays live statistics on feed volume and rate.
- **Service** - an ASP.NET Web API project that consumes a tweet stream (twitter or emitter) in a background service
	and exposes an API to get statistics for elapsed time, tweet count and the top 10 hashtags (and their count of occurance)
- **Service.Tests** - a class library of XUnit Unit tests for the services in the Service project (with 100% coverage, though I could think of more esoteric tests)
- **Web** - a Blazor WASM web project that polls the Service API and displays the statistics in real-time

	
## How to Run the solution

### To execute this project using the Emitter test stream:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.Service project, edit the appsettings.json file
3. Ensure the TwitterConnection:Url line with twitter.com is commented out and the one with localhost is uncommented. Note that the AccessToken is ignored by the emitter, so if you already added it, you can leave it. 
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. Change the Action next to the following projects to "Start": JackHenry.TwitterScan.Emitter, JackHenry.TwitterScan.Service and JackHenry.TwitterScan.Web
7. Build and Run. In addition to the three consoles, two browser windows will open: 
		one with the Swagger page for the API and another with 
		a Blazor WASM app that polls the API every 500ms and displays the results 

Note that the throttling for the emitter as called by the service is set to 400 Kilotweets per second. 
	To adjust that, edit the rate query parameter of the TwitterConnection:Url in JackHenry.TwitterScan.Service/appsettings.json

### To execute the project using the live Twitter stream:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.Service project, edit the appsettings.json file
3. Set TwitterConnection:AccessToken to your valid Twitter Bearer token and
	Ensure the TwitterConnection:Url line with twitter.com is uncommented and the one with localhost is commented out
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. Change the Action next to the following projects to "Start": JackHenry.TwitterScan.Service and JackHenry.TwitterScan.Web
7. Build and Run. In addition to the three consoles, two browser windows will open: one with the Swagger page for the API and another with a Blazor WASM app that polls the API every 500ms and displays the results 

### To execute the POC Console:
1. Open the solution using Visual Studio 2022
2. In the solution explorer, open the JackHenry.TwitterScan.PocConsole project, edit the appsettings.json file
3. Set TwitterAccessToken to your valid Twitter Bearer token
4. Right-click on the Solution in the Solution Explorer and select "Set Startup Projects..."
5. Select the Multiple startup projects option
6. If you plan to use the mock emitter, change the Action next to the following projects to "Start": JackHenry.TwitterScan.Emitter and JackHenry.TwitterScan.PocConsole. Otherwise, just set PocConsole as the single project to start
7. Build and Run. The console window will open, prompt you to choose live twitter feed (default) or set an Emitter rate. It will then display 2 lines: a header line and value line with the elapsed time in seconds, tweet count and rate of tweets per second received which will update in place in real time.
	
## Notes

### Emitter Phases
I built in a series of phases that the emitter goes through in the first minute. First it's just picking from tags at random, then it focuses on a specific set of tags for a few seconds, finally it focuses on only tags not in that set. The head start the selected set gets is more dramatic the higher the streaming rate.

### Balance of Illustration vs Pragmatism
I struggled with finding balance between concise organization verses illustrating techniques I would employ for less trivial systems. In the end, I hope I found a balance that is acceptable. One area I stopped short of was API Versioning. It's critical to consider this when developing RESTful APIs from the beginning. I theoretically could have broken the classes down to absurdity, but what I have is representative of the choices I would make in a real system. I felt trying to turn this little project into a SOLID/Design Pattern tour-de-force would make me out to be an "Architecture Astronaut", and that would be misrepenting myself. I'm actually quite pragmatic, so I just applied what techniques seemed appropriate to the limited scope. But, if Architecture Astronautics **is** desired, I'm always happy to get up in the atmosphere, too.

### Scaling
I approached the problem agnostic of the load the stream would encounter - I built it to handle a stream at the limits of the hardware where the solution is deployed. My Emitter can blast out about 680,000 tweets a second (as measured with the POCConsole) and the server handles 400,000/s locally without issue. Given that the expectation was that Twitter as a whole only generates 5,700 tweets per second currently, I think there isn't really a future scaling issue that cannot be solved using infrastructure for this particular toy problem. 
But, assuming for a moment there was a scaling challenge on the horizon, I don't see any way in-process to make this drastically more efficient. My solution to scale further would go something like this: 
1. I would move to a parallel architecture with multiple Tweet Stream Reader servers on multiple machines feeding a single Tweet Statistics Repository server.
2. I would need to partition the queries for the streams in some way to ensure the Tweet Stream Reader servers were not receiving duplicate tweets. 
3. Each Tweet Stream Reader server would aggregate count and count per hashtag locally for it's stream, as the current solution does, and periodically send an update of those stats (all hashtags encountered, not just the top ten) to the Tweet Statistics Repository server that would aggregate those updates to its own accounting - which the API would report from for aggregated stats.
4. The API to access the stats would sit on the Tweet Statistics Repository server. If we needed to scale the API access for massive numbers of queries, a web farm where each Tweet Statistics Repository server is on the backbone maintaining its own aggregate stats would scale that end. Since the frequency of statistics change is slower, caching could also be utilized in the infrastructure level to just return the last response to all callers with a cache invalidation every 500ms.

### Extensibility vs Performance
My initial focus was on performance, processing as many tweets as possible as fast as possible. I may have picked that up from the tone of the prompt, or maybe it's my own bias as a "code hot-rodder". I considered switching to the above described architecture, but given my time constraints and that I feel what I've produced demonstrates my abilities well enough for evaluation, I decided to include this section in the readme instead. Normally, this would be a design decision we discuss at the onset of a project. What's more important to our long-term needs: catching every tweet no matter the future volume or extensibility of function? I chose the former as the hypothetical answer I received.

I considered the extensibility and OCP issues of the project. The expansion points which I anticipate are processing additional information from the tweets and consuming different kinds of input streams (other social networks, etc.). There is a refactor here that leads to a much more expandable and more general solution whereby there is a set of Stream Reader Services for each input stream and a set of Statistics Repository equivalents - lets call those Message Processors - that may be able to consume messages from multiple input streams (perhaps a running total of top hashtags across networks). Perhaps the different Stream Reader Services and Message Processors are discovered by a reflection search by a specific attribute or interface on start up and stored in a Stream Reader Service Repository and a Message Processor Repository where the stream readers are fed the kinds of messages they claim to support. Architecture Astronaut fun for sure.

Another path not taken is using reflection to detect services to be registered for IoC. I have my own framework I use for this that almost completely avoids ever needing to modify the program.cs code. Adds another class to OCP compliance, but the way I did it is the "Microsoft way" - just not the best way.

The razor for me on this project with performance vs OCP compliance is the Tweet data objects. To support multiple statistics processors, I would need to capture all the data possibly streamed for each tweet. Or perhaps iterate through the Message Processors so they can add their needs to the request query in some kind of visitor pattern. Looking at the Twitter API docs - the total possible data in each tweet can get quite extensive. One detail of my performance-focused approach was to shape the Tweet data objects to match only the parts of the tweet stream which we use, which is for now, only the hashtags. This effectively discards the unused data in the deserializer and skips a lot of wasted cycles in the stream reader loop deserializing unneeded data. Granted all the above could be implemented and just the Tweet data objects become OCP failures... but if you're not going to truly decouple the system, it's dangerous to only decouple part of it and give a false illusion of extensibility. 


