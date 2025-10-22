# ProjectHorizon

## Description
Imagine a world where images are processed and transformed effortlessly in the cloud.  
_Project Horizon_ is an image processing pipeline that optimize images uploaded by users, making them smaller and faster to load without sacrificing visual quality.

## System Requirements

### Functional Requirements
- The service is automatically triggered when an image is uploaded to a source storage. (eg an object storage).
- The service processes the image uploaded to the source storage and uploads it to a destination storage.
- The processed image must be lighter and faster to load.
- The system must ensure good quality of the processed images.

### Non-Functional Requirements
- The system ensures efficient image processing to minimize execution time and costs.
- The system includes basic error handling to ensure the robustness of your function.

## System Design

### Infrastructure
As the architecture, I implemented a serverless architecture on AWS as it meets all functional and non-functional requirements.  
- Despite the limited time available, IaC tools allow for the setup of the infrastructure with minimal effort.
- AWS events are easy to leverage to implement an event-driven asynchronous system.
- With the AWS Lambda service, we ensure efficiency and low costs thanks to the pay-per-use pricing model.

The system leverages Lambda and S3 services to operate and can be interacted with through CLIs, applications using the AWS SDK, or clients that interact with the system via HTTP and an API Gateway if the application needs to scale.
<br>
<br>
Given the limited time, **the system can only be interacted with through the CLI for now**.

![project-horizon.drawio.png](docs/project-horizon.drawio.png)

## Balance between speed, storage, and image quality.
The system uses the [SixLabors.ImageSharp](https://docs.sixlabors.com/articles/imagesharp/index.html?tabs=tabid-1) library to manipulate images and is configured to balance storage efficiency and image quality.  
<br>
The maximum size of the optimized images is 1.000x1.000 pixels, and the Lanczos algorithm is used for downscaling, ensuring good image quality. You can configure it in the `appSettings.json` file by modifying the `MaxImageDimension` property.
<br><br>
To prevent memory issues on Lambda, the property `MaxImageSizeInBytes` in the `appSettings.json` file has been configured to set the maximum image size (in bytes) for processing. If a larger image is uploaded to the source storage bucket, it will not be processed by the Lambda.
<br><br>
As the conversion algorithm, WebP in Lossless mode has been used. WebP is a good choice because it offers efficient compression without compromising image quality and is supported by all major web browsers.

### Benchmarks (Maximum image dimension: 1000 pixels)
| Filename             | Original Dimension (pixels) | Original Size (MB) | Final Dimension (pixels) | Final Size (MB) | Lambda Billed Duration (ms) |
|----------------------|-----------------------------|--------------------|--------------------------|-----------------|-----------------------------|
| source_800_800.png   | 800x800                     | 1.3                | 800x800                  | 0.77            | 7228                        |
| source_1920_1080.png | 1920x1080                   | 3.9                | 1000x562                 | 0.70            | 7632                        |


## How to deploy and run Project Horizon

For deploying and running Project Horizon, you need to have:

- [An AWS account](https://aws.amazon.com/account/?nc1=h_ls)
- [AWS CLI configured](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-getting-started.html)

### Deploy the infrastructure to AWS
- From the root of the project run `./deploy.sh` (default region: `eu-central-1`, default env: `dev`)
- Example for a custom deployment `./deploy.sh us-west-2 prod`

### Invoke the image optimizer
- Upload an image on the source S3 bucket.
    - Run `aws s3 cp <local_path>/image_name.png s3://<YOUR_AWS_ACCOUNT_ID>-source-bucket`
- Download the optimized image from the destination S3 bucket.
    - Run `aws s3 cp s3://<YOUR_AWS_ACCOUNT_ID>-destination-bucket/image_name.webp <local_path> `

There are some images provided for you inside the `SourceImages` folder.

### Delete the infrastructure
- From the root of the project run `./delete.sh` (default region: `eu-central-1`, default env: `dev`)

## How to run unit tests
- Run `dotnet test`
