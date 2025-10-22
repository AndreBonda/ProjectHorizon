#!/bin/bash
set -e

# Set variables
REGION=${1:-eu-central-1}
ENVIRONMENT=${2:-dev}
STACK_NAME="project-horizon-$ENVIRONMENT"
echo "Deleting stack..."
echo "Region ($REGION), Environment ($ENVIRONMENT), STACK: ($STACK_NAME)"

# Get account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
DEPLOYMENT_BUCKET="$AWS_ACCOUNT_ID-deployment-bucket"
echo "AWS Account ID: $AWS_ACCOUNT_ID"
echo "Deployment Bucket: $DEPLOYMENT_BUCKET"

# Empty S3 buckets (con gestione errori)
echo "Removing bucket contents..."
aws s3 rm s3://$DEPLOYMENT_BUCKET --recursive 2>/dev/null || echo "Deployment bucket already empty or doesn't exist"
aws s3 rm s3://$AWS_ACCOUNT_ID-source-bucket --recursive 2>/dev/null || echo "Source bucket already empty or doesn't exist"
aws s3 rm s3://$AWS_ACCOUNT_ID-destination-bucket --recursive 2>/dev/null || echo "Destination bucket already empty or doesn't exist"

# Removing Stack
echo "Removing CloudFormation stack..."
aws cloudformation delete-stack \
  --stack-name $STACK_NAME \
  --region $REGION

echo "Waiting for stack deletion to complete..."
aws cloudformation wait stack-delete-complete \
  --stack-name $STACK_NAME \
  --region $REGION

echo "Stack deleted successfully!"