namespace ImageOptimizerLambda.Exceptions;

public class ImageDownloadFromSourceBucketException(string message, Exception innerException) : Exception(message, innerException);