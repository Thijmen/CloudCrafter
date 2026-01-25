import { useDeleteStackHook } from '@cloudcrafter/api'
import { Button } from '@cloudcrafter/ui/components/button'
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@cloudcrafter/ui/components/dialog'
import { toast } from 'sonner'

interface DeleteStackDialogProps {
	isOpen: boolean
	onClose: () => void
	onConfirm: () => void
	stackDetails: {
		name: string
		id: string
	}
}

export const DeleteStackDialog = ({
	isOpen,
	onClose,
	onConfirm,
	stackDetails,
}: DeleteStackDialogProps) => {
	const mutation = useDeleteStackHook({
		mutation: {
			onSuccess: () => {
				toast.success('Stack deleted successfully')
				onConfirm()
			},
		},
	})

	const handleDeleteStack = () => {
		mutation.mutate({ id: stackDetails.id })
	}

	return (
		<Dialog open={isOpen} onOpenChange={onClose}>
			<DialogContent>
				<DialogHeader>
					<DialogTitle>Delete Stack</DialogTitle>
					<DialogDescription>
						Are you sure you want to delete the stack "{stackDetails.name}"?
						This action cannot be undone and will permanently delete the stack
						and all related entries.
					</DialogDescription>
				</DialogHeader>
				<DialogFooter>
					<Button
						disabled={mutation.isPending}
						variant='outline'
						onClick={onClose}
					>
						Cancel
					</Button>
					<Button
						disabled={mutation.isPending}
						variant='destructive'
						data-testid='button-stack-delete-confirm'
						onClick={handleDeleteStack}
					>
						{mutation.isPending ? 'Deleting...' : 'Delete Stack'}
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	)
}
