#!/bin/bash
set -e

# Set variables
REGION=${1:-eu-central-1}
ENVIRONMENT=${2:-dev}
STACK_NAME="project-horizon-$ENVIRONMENT"
echo "Deploying stack..."
echo "Region ($REGION), Environment ($ENVIRONMENT), STACK: ($STACK_NAME)"

# Get account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
DEPLOYMENT_BUCKET="$AWS_ACCOUNT_ID-deployment-bucket"
echo "AWS Account ID: $AWS_ACCOUNT_ID"
echo "Deployment Bucket: $DEPLOYMENT_BUCKET"

# Project compilation
echo "Building project..."
dotnet publish ImageOptimizerLambda/src/ImageOptimizerLambda/ImageOptimizerLambda.csproj \
  -c Release \
  -o ./publish \
  --self-contained false \
  -r linux-x64

echo "Creating deployment package..."
cd publish
zip -r ../lambda-deployment.zip . -q
cd ..

# Create deployment bucket if does not exist
echo "Checking deployment bucket..."
if ! aws s3 ls "s3://$DEPLOYMENT_BUCKET" 2>/dev/null; then
    echo "Creating deployment bucket..."
    aws s3 mb "s3://$DEPLOYMENT_BUCKET" --region $REGION
    echo "Bucket created: $DEPLOYMENT_BUCKET"
else
    echo "Bucket exists: $DEPLOYMENT_BUCKET"
fi

# Upload code
echo "Uploading code to S3..."
aws s3 cp lambda-deployment.zip "s3://$DEPLOYMENT_BUCKET/lambda-deployment.zip" --region $REGION
echo "Code uploaded to S3"

# CloudFormation deploy
echo "Deploying CloudFormation stack..."
aws cloudformation deploy \
  --template-file ImageOptimizerLambda/src/ImageOptimizerLambda/template.yaml \
  --stack-name $STACK_NAME \
  --capabilities CAPABILITY_IAM \
  --region $REGION \
  --no-fail-on-empty-changeset

# Get outputs
echo "Stack outputs:"
aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --region $REGION \
  --query 'Stacks[0].Outputs[*].[OutputKey,OutputValue]' \
  --output table 2>/dev/null || echo "No outputs found"

echo "🧹 Cleaning up..."
rm -f lambda-deployment.zip
rm -rf publish

echo "Deployment completed successfully!"
echo "To test: upload an image to s3://$AWS_ACCOUNT_ID-source-bucket"
