namespace ImageOptimizerLambda.Exceptions;

public class ImageUploadToDestinationBucketException(string message, Exception innerException) : Exception(message, innerException);