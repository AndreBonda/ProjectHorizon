#!/bin/bash
set -e

# ===== Set variables =====
REGION=${1:-eu-central-1}
ENVIRONMENT=${2:-dev}
STACK_NAME="project-horizon-$ENVIRONMENT"
echo "Deploying stack..."
echo "Region ($REGION), Environment ($ENVIRONMENT), STACK: ($STACK_NAME)"

# ===== Get account ID =====
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
DEPLOYMENT_BUCKET="$AWS_ACCOUNT_ID-deployment-bucket"
echo "AWS Account ID: $AWS_ACCOUNT_ID"
echo "Deployment Bucket: $DEPLOYMENT_BUCKET"

# ===== Create deployment bucket if does not exist =====
echo "Checking deployment bucket..."
if ! aws s3 ls "s3://$DEPLOYMENT_BUCKET" 2>/dev/null; then
    echo "Creating deployment bucket..."
    aws s3 mb "s3://$DEPLOYMENT_BUCKET" --region $REGION
    echo "Bucket created: $DEPLOYMENT_BUCKET"
else
    echo "Bucket exists: $DEPLOYMENT_BUCKET"
fi

# ===== Image Optimizer Lambda deloyment =====
echo "[ImageOptimizerLambda] - Building project..."
dotnet publish ImageOptimizerLambda/src/ImageOptimizerLambda/ImageOptimizerLambda.csproj \
  -c Release \
  -o ./publish-optimizer \
  --self-contained false \
  -r linux-x64

echo "[ImageOptimizerLambda] - Creating deployment package..."
cd publish-optimizer
zip -r ../lambda-optimizer-deployment.zip . -q
cd ..

echo "[ImageOptimizerLambda] - Uploading code to S3..."
aws s3 cp lambda-optimizer-deployment.zip "s3://$DEPLOYMENT_BUCKET/lambda-optimizer-deployment.zip" --region $REGION
echo "[ImageOptimizerLambda] - Code uploaded to S3"

# ===== Image Url Generator Lambda dployment =====
echo "[ImageUrlGeneratorLambda] - Building project..."
dotnet publish ImageUrlGeneratorLambda/src/ImageUrlGeneratorLambda/ImageUrlGeneratorLambda.csproj \
  -c Release \
  -o ./publish-url-generator \
  --self-contained false \
  -r linux-x64

echo "[ImageUrlGeneratorLambda] - Creating deployment package..."
cd publish-url-generator
zip -r ../lambda-url-generator-deployment.zip . -q
cd ..

echo "[ImageUrlGeneratorLambda] - Uploading code to S3..."
aws s3 cp lambda-url-generator-deployment.zip "s3://$DEPLOYMENT_BUCKET/lambda-url-generator-deployment.zip" --region $REGION
echo "[ImageUrlGeneratorLambda] - Code uploaded to S3"

# CloudFormation deploy
echo "Deploying CloudFormation stack..."
aws cloudformation deploy \
  --template-file template.yaml \
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
  
# ===== Force Lambda code update =====
echo "Force updating Lambda function code..."

# Update ImageOptimizer  
aws lambda update-function-code \
  --function-name ImageOptimizer \
  --s3-bucket "$DEPLOYMENT_BUCKET" \
  --s3-key "lambda-optimizer-deployment.zip" \
  --region $REGION \
  > /dev/null

# Update ImageUrlGenerator
aws lambda update-function-code \
  --function-name ImageUrlGenerator \
  --s3-bucket "$DEPLOYMENT_BUCKET" \
  --s3-key "lambda-url-generator-deployment.zip" \
  --region $REGION \
  > /dev/null

echo "✅ Lambda code force updated!"

echo "🧹 Cleaning up..."
rm -f lambda-optimizer-deployment.zip
rm -rf publish-optimizer
rm -f lambda-url-generator-deployment.zip
rm -rf publish-url-generator

echo "Deployment completed successfully!"
echo "To test: upload an image to s3://$AWS_ACCOUNT_ID-source-bucket"
