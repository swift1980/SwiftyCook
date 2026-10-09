/** Extracts a human-readable message from an unknown error (e.g. an Axios/API error), falling back otherwise. */
export function getErrorMessage(err: unknown, fallback: string): string {
  // Prefer the API's own message (e.g. a 409 usage summary) over the generic Axios one.
  const apiMessage = (err as { response?: { data?: { message?: unknown } } } | null)?.response?.data
    ?.message
  if (typeof apiMessage === 'string' && apiMessage) return apiMessage

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
