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

## How to deploy and run Project Horizon
For deploying and running Project Horizon, you need to have:  
- [An AWS account](https://aws.amazon.com/account/?nc1=h_ls)
- [AWS CLI configured](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-getting-started.html)
- [SAM (Serverless Application Model)](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html#install-sam-cli-instructions)

#### Build artifacts
- `cd ImageOptimizerLambda/src/ImageOptimizerLambda`
- `sam build`
### Deploy the infrastructure to AWS
- Run `sam deploy --guided` the first time, or `sam deploy` for subsequent deployments.
#### SAM configurations (only the first time)
During the deployment, you will be asked for some parameters:
- Stack Name [sam-app]: `insert a stack name (es. 'project-horizon')`
- AWS Region [us-east-1]: `insert an AWS Region (es. 'us-east-1')`
- Confirm changes before deploy [y/N]: `insert: 'n'`
- Allow SAM CLI IAM role creation [Y/n]: `insert 'y'`
- Disable rollback [y/N]: `insert 'n'`
- Save arguments to configuration file [Y/n]: `insert 'y'`
- SAM configuration file [samconfig.toml]: `insert a stack name (es. 'samconfig.toml')`
- SAM configuration environment [default]: `insert a configuration environment (es. 'default')`

### Delete the infrastructure
- Run `aws s3 rm s3://<AWS_ACCOUNT_ID>-source-bucket --recursive` to empty the source S3 bucket.
- Run `aws s3 rm s3://<AWS_ACCOUNT_ID>-destination-bucket --recursive` to empty the destination S3 bucket.
- Run `sam delete` to delete the infrastructure.
