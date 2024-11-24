namespace ImageOptimizerLambda.Exceptions;

public class ImageOptimizationException(string message, Exception innerException) : Exception(message, innerException);