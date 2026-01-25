import type { APIRequestContext, Page } from '@playwright/test'
import { test as base, expect } from './playwright-fixture'
import { UserFixture } from './user-fixture'
export * from '@playwright/test'
import { isAxiosError } from 'axios'
import type { ProjectDto, StackCreatedDto } from '../__generated__'
import {
	deleteProject as apiDeleteProject,
	createProject,
	createServer,
	createStackBuildOptionEnum,
	createTeam,
	deleteTeam,
	getProjects,
	getServers,
	postCreateStack,
} from '../__generated__'
import { generateProjectName, generateStackName } from '../utils/fake-data'

/**
 * Project fixture that extends UserFixture with project creation capabilities
 */
export class ProjectFixture extends UserFixture {
	projects: ProjectDto[] = []
	teams: string[] = []

	/**
	 * Clean up all created projects
	 */
	async cleanupProjects(): Promise<void> {
		if (this.projects.length === 0) return

		// Delete each project in reverse order (newest first)
		for (const project of [...this.projects].reverse()) {
			try {
				await apiDeleteProject(project.id, this.getApiClientConfig())
			} catch (error) {
				// Ignore 404 errors (project already deleted)
				if (isAxiosError(error) && error.response?.status === 404) {
					// Project already deleted, just continue
					continue
				}
				console.error(`Failed to clean up project ${project.name}:`, error)
			}
		}

		for (const team of this.teams) {
			try {
				await deleteTeam(team, this.getApiClientConfig())
			} catch (error) {
				console.error(`Failed to clean up team ${team}:`, error)
			}
		}

		this.teams = []
		this.projects = []
	}

	async fixtureCreateTeam(teamName: string): Promise<string> {
		const team = await createTeam({ name: teamName }, this.getApiClientConfig())
		this.teams.push(team)
		return team
	}

	async fixtureCreateProject(projectName: string): Promise<ProjectDto> {
		const team = await createTeam(
			{
				name: `Team ${projectName}`,
			},
			this.getApiClientConfig(),
		)

		this.teams.push(team)

		const result = await createProject(
			{ name: projectName, teamId: team },
			this.getApiClientConfig(),
		)

		this.projects.push(result)

		// fetch latest project
		const projects = await getProjects(
			{ includeEnvironments: true },
			this.getApiClientConfig(),
		)
		const project = projects.find((p) => p.id === result.id)
		if (!project) {
			throw new Error(`Project ${result.name} not found after creation`)
		}

		return project
	}

	async fixtureCreateStack(project: ProjectDto): Promise<StackCreatedDto> {
		expect(project.environments.length).toBeGreaterThan(0)
		expect(project.environments[0].id).toBeDefined()

		const team = await createTeam(
			{
				name: `Team ${project.name}`,
			},
			this.getApiClientConfig(),
		)

		this.teams.push(team)

		const server = await createServer(
			{
				name: `Server ${project.name}`,
				teamId: team,
			},
			this.getApiClientConfig(),
		)

		const client = this.getApiClientConfig()

		const servers = await getServers(client)
		expect(servers.length).toBeGreaterThan(0)
		expect(servers[0].id).toBeDefined()

		const result = await postCreateStack(
			{
				environmentId: project.environments[0].id,
				name: generateStackName(),
				gitRepository: 'https://github.com/cloudcrafter-oss/demo-examples',
				serverId: servers[0].id,
				gitBranch: 'main',
				pathInGitRepository: '/',
				buildOption: createStackBuildOptionEnum.Nixpacks,
			},
			client,
		)

		return result
	}
}

export interface StackDetails {
	stack: StackCreatedDto
	project: ProjectDto
}

/**
 * Extend test with project fixture that provides an authenticated user and a created project
 */
export const test = base.extend<{
	project: ProjectDto
	page: Page
	projectFixture: ProjectFixture
	request: APIRequestContext
	stack: StackDetails
}>({
	project: async ({ browser, page, request }, use) => {
		// Create project fixture
		const projectFixture = new ProjectFixture(page, request)

		await projectFixture.loginViaApi()

		const projectName = generateProjectName()

		const project = await projectFixture.fixtureCreateProject(projectName)

		// Make fixture available for use in tests
		await use(project)

		// Clean up projects after test
		await projectFixture.cleanupProjects()
	},

	projectFixture: async ({ browser, page, request }, use) => {
		// Create project fixture
		const projectFixture = new ProjectFixture(page, request)

		await projectFixture.loginViaApi()

		// Make fixture available for use in tests
		await use(projectFixture)
	},

	stack: async ({ projectFixture, project }, use) => {
		// Make fixture available for use in tests

		const result: StackDetails = {
			project,
			stack: await projectFixture.fixtureCreateStack(project),
		}
		await use(result)
	},
})
