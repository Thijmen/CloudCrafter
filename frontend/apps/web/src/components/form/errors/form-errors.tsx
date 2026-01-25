import type { FieldErrors } from 'react-hook-form'

export const FormErrors = ({ errors }: { errors: FieldErrors }) => {
	if (process.env.NODE_ENV === 'development') {
		return <pre>{JSON.stringify(errors, null, 2)}</pre>
	}
	return null
}
