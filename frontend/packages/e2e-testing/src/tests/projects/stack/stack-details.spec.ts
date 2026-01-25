import { AxiosError } from 'axios'
import { getStackDetail } from '../../../__generated__'
import { expect, test } from '../../../fixtures/project-fixture'
import { StackDetailPage } from '../../../pages/projects/stack/stack.detail'

test('should be able to see a basic stack', async ({
	page,
	projectFixture,
	stack,
}) => {
	const stackDetailsPage = StackDetailPage(page)

	await stackDetailsPage.goToStackOverview(stack)

	await expect(page.getByText('Basic details about your Stack')).toBeVisible()
})

test('should be able to delete a stack', async ({
	page,
	projectFixture,
	stack,
}) => {
	const stackDetailsPage = StackDetailPage(page)

	await stackDetailsPage.goToStackOverview(stack)

	await expect(page.getByText('Basic details about your Stack')).toBeVisible()

	await stackDetailsPage.goToAdvancedStackTab()

	await expect(page.getByText('Advanced settings for your Stack')).toBeVisible()

	await expect(stackDetailsPage.advanced.deleteStackButton).toBeVisible()

	await stackDetailsPage.advanced.deleteStackButton.click()

	await expect(
		page.getByText('Are you sure you want to delete the stack'),
	).toBeVisible()

	await expect(stackDetailsPage.advanced.confirmDeleteStackButton).toBeVisible()

	const stackDetailsFromApi = await getStackDetail(
		stack.stack.id,
		projectFixture.getApiClientConfig(),
	)

	expect(stackDetailsFromApi).toBeDefined()

	await stackDetailsPage.advanced.confirmDeleteStackButton.click()

	await expect(page.getByText('Stack deleted successfully')).toBeVisible()

	await stackDetailsPage.waitForProjectPage(stack)

	try {
		const stackDetailsFromApi = await getStackDetail(
			stack.stack.id,
			projectFixture.getApiClientConfig(),
		)
	} catch (error) {
		if (error instanceof AxiosError) {
			expect(error.response?.status).toBe(401)
		}
	}
})
