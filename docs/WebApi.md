# Usage

The WebApi enables applications to interact with the lobby programmatically.  

It's intended to allow users to write their own control panels, automate actions and collect telemetry and performance data.

# Configuration
  
It's usage is controller by three config variables  
  
`EnableWebApi = <true/false>`: If not enabled, everything related to the WebApi is disabled and the following settings won't have any effects

`EnableSwagger = <true/false>`: If enabled, it serves a webpage at `<WebApiUrl>/swagger` that allows you to test the API manually. You can read more about Swagger at https://swagger.io/

`WebApiUrl = <URL>`: Sets the endpoint where the WebApi should listen for requests. The default is `http://localhost:5000`. That tells the API to listen for requests only from the local machine on the TCP port 5000.

# Security

> [!CAUTION]
> DO NOT EXPOSE THE WEB API TO THE INTERNET.

The WebApi gives **full control** of the internal functionality of the lobby, so **it features no security**.  

Developers wishing to give their users access to data or functionality provided by the WebApi should consume the api internally and exposing its data and functionality through proper access control.