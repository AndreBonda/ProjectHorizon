namespace ImageOptimizerLambda.Exceptions;

public class DownloadPresignedUrlGenerationException(string message, Exception innerException) : Exception(message, innerException);
public class ImageDownloadFromSourceBucketException(string message, Exception innerException) : Exception(message, innerException);
public class ImageOptimizationException(string message, Exception innerException) : Exception(message, innerException);
public class ImageUploadToDestinationBucketException(string message, Exception innerException) : Exception(message, innerException);
public class RecordImageProcessingException(string message, Exception innerException) : Exception(message, innerException);
public class TooLargeImageException(string message) : Exception(message);