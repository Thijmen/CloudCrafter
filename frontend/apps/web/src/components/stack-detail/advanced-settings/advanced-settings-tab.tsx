import type { StackDetailDto } from '@cloudcrafter/api'
import { Button } from '@cloudcrafter/ui/components/button'
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@cloudcrafter/ui/components/card'
import { useParams, useRouter } from 'next/navigation'
import { useState } from 'react'
import { DeleteStackDialog } from './delete-stack-dialog'

export const AdvancedSettingsTab = ({
	stackDetails,
}: { stackDetails: StackDetailDto }) => {
	const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
	const router = useRouter()
	const params = useParams()
	const { 'project-uuid': projectId, 'environment-uuid': environmentId } =
		params

	const handleDeleteStack = () => {
		router.push(`/admin/projects/${projectId}/${environmentId}`)
	}

	return (
		<div className='space-y-6'>
			<Card className='border-red-500 bg-red-50'>
				<CardHeader className='relative'>
					<CardTitle className='text-red-700'>Advanced Settings</CardTitle>
					<CardDescription className='text-red-600'>
						Advanced settings for your Stack
					</CardDescription>
				</CardHeader>

				<CardContent className='space-y-4'>
					<div className='border-t border-red-200 pt-4'>
						<h3 className='text-lg font-semibold text-red-700'>Delete Stack</h3>
						<p className='text-sm text-red-600 mb-4'>
							This action is not reversible. All stack data and related entries
							will be permanently deleted.
						</p>
						<Button
							data-testid='button-delete-stack'
							variant='destructive'
							onClick={() => setIsDeleteDialogOpen(true)}
						>
							Delete Stack
						</Button>
					</div>
				</CardContent>
			</Card>

			<Card>
				<CardContent>
					<pre>
						{JSON.stringify(
							{ projectId, environmentId, stackDetails },
							null,
							2,
						)}
					</pre>
				</CardContent>
			</Card>

			<DeleteStackDialog
				isOpen={isDeleteDialogOpen}
				onClose={() => setIsDeleteDialogOpen(false)}
				onConfirm={handleDeleteStack}
				stackDetails={{
					name: stackDetails.name,
					id: stackDetails.id,
				}}
			/>
		</div>
	)
}
