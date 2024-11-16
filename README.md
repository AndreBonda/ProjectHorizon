# ProjectHorizon
## Description
Imagine a world where images are processed and transformed effortlessly in the cloud.  
_Project Horizon_ is an image processing pipeline that optimize images uploaded by users, making them smaller and faster
to load without sacrificing visual quality.
## System Design

### Functional Requirements
- The service is automatically triggered when an image is uploaded to a source storage. (eg an object storage).
- The service processes the image uploaded to the source storage and uploads it to a destination storage.
- The processed image must be lighter and faster to load.

### Non-Functional Requirements
- The system ensures efficient image processing to minimize execution time and costs.
- The system includes basic error handling to ensure the robustness of your function.

### Constraints
- Adopt .Net Core and/or Java as the technology stack.
- The time limit for designing and implementing the system is 10 hours.

### Infrastructure
Here is a possible complete design of the infrastructure.  
The system can be interacted with through CLIs, applications using the AWS SDK, or clients that interact with the system
via HTTP and an API Gateway if the application needs to scale.  
Given the limited time, **the system can only be interacted with through the CLI for now**.  
![project-horizon.drawio.png](docs/project-horizon.drawio.png)