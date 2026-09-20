/** Extracts a human-readable message from an unknown error (e.g. an Axios/API error), falling back otherwise. */
export function getErrorMessage(err: unknown, fallback: string): string {
  if (
    err &&
    typeof err === 'object' &&
    'message' in err &&
    typeof (err as { message?: unknown }).message === 'string'
  ) {
    return (err as { message: string }).message
  }
  return fallback
}
