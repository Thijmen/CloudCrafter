'use client'

import { SourceSettings } from '@/src/components/stack-detail/source-settings'
import { useStackHub } from '@/src/hooks/useStackHub'
import type { StackDetailDto } from '@cloudcrafter/api'
import { useGetDeploymentsForStackHook } from '@cloudcrafter/api'
import { Key, List, type LucideIcon, Server, Settings } from 'lucide-react'
import type React from 'react'
import { useEffect, useState } from 'react'
import { AdvancedSettingsTab } from '../advanced-settings/advanced-settings-tab'
import { DeploymentList } from '../deployments/deployment-list'
import { DockerStorage } from '../docker-storage/DockerStorage'
import { EnvironmentVariables } from '../environment-variables/EnvironmentVariables'
import { VariableHistory } from '../environment-variables/VariableHistory'
import { VariableTemplates } from '../environment-variables/VariableTemplates'
import { BasicInfo } from '../general-settings/BasicInfo'
import { ServiceOverview } from '../service-overview'

// Define the base props interface
interface BaseComponentProps {
	stackDetails: StackDetailDto
}

interface NavItem {
	id: string
	title: string
	testId?: string
	icon: LucideIcon
	children: {
		id: string
		title: string
		testId?: string
	}[]
}

// Update the sections structure
const sections = [
	{
		id: 'general',
		title: 'General Settings',
		testId: 'general-settings',
		icon: Settings,
		subTabs: [
			{
				id: 'basic',
				title: 'Basic Info',
				component: BasicInfo as React.ComponentType<BaseComponentProps>,
			},
			{
				id: 'source',
				title: 'Source',
				component: SourceSettings as React.ComponentType<BaseComponentProps>,
			},
			{
				id: 'advanced',
				title: 'Advanced Settings',
				component:
					AdvancedSettingsTab as React.ComponentType<BaseComponentProps>,
			},
		],
	},
	{
		id: 'deployments',
		title: 'Deployments',
		testId: 'subsection-deployments',
		icon: List,
		subTabs: [
			{
				id: 'list',
				title: 'Deployment List',
				component: ({ stackDetails }: BaseComponentProps) => {
					const { data: deployments = [] } = useGetDeploymentsForStackHook(
						stackDetails.id,
					)
					return <DeploymentList deployments={deployments} />
				},
			},
		],
	},
	{
		id: 'environment',
		title: 'Environment Variables',
		testId: 'subsection-environment-variables',
		icon: Key,
		subTabs: [
			{
				id: 'variables',
				title: 'Variables',
				component:
					EnvironmentVariables as React.ComponentType<BaseComponentProps>,
			},
			{
				id: 'templates',
				title: 'Templates',
				component: VariableTemplates as React.ComponentType<BaseComponentProps>,
			},
			{
				id: 'history',
				title: 'History',
				component: VariableHistory as React.ComponentType<BaseComponentProps>,
			},
		],
	},
	{
		id: 'services',
		title: 'Services',
		testId: 'subsection-services',
		icon: Server,
		subTabs: [
			{
				id: 'overview',
				title: 'Overview',
				component: ServiceOverview as React.ComponentType<BaseComponentProps>,
			},
			{
				id: 'storage',
				title: 'Docker Storage',
				component: DockerStorage as React.ComponentType<BaseComponentProps>,
			},
		],
	},
]

export const navItems: NavItem[] = sections.map((section) => ({
	id: section.id,
	title: section.title,
	icon: section.icon,
	testId: section.testId,
	children: section.subTabs.map((subTab) => ({
		id: subTab.id,
		title: subTab.title,
		testId: `subtab-${subTab.id}`,
	})),
}))

const StackConfigPage: React.FC<{ stackDetails: StackDetailDto }> = ({
	stackDetails,
}) => {
	const [activeSection, setActiveSection] = useState(sections[0].id)
	const [activeSubTab, setActiveSubTab] = useState(sections[0].subTabs[0].id)

	const { stack } = useStackHub({ initialStack: stackDetails })

	useEffect(() => {
		// Parse the hash on component mount and when it changes
		const handleHashChange = () => {
			const hash = window.location.hash.slice(1) // Remove the '#' character
			const [sectionId, subTabId] = hash.split('/')

			if (sectionId) {
				const section = sections.find((s) => s.id === sectionId)
				if (section) {
					setActiveSection(sectionId)
					if (subTabId && section.subTabs.some((st) => st.id === subTabId)) {
						setActiveSubTab(subTabId)
					} else {
						setActiveSubTab(section.subTabs[0].id)
					}
				}
			}
		}

		handleHashChange() // Handle initial hash
		window.addEventListener('hashchange', handleHashChange)
		return () => window.removeEventListener('hashchange', handleHashChange)
	}, [])

	return (
		<div className='flex flex-col md:flex-row min-h-0 max-w-[100vw]'>
			{/* Content Area */}
			<div className='flex-1 min-w-0 overflow-y-auto overflow-x-hidden'>
				<div className='w-full max-w-full pt-0 md:pt-0 p-4 md:p-6'>
					{sections.map((section) =>
						section.subTabs.map(
							(subTab) =>
								activeSection === section.id &&
								activeSubTab === subTab.id && (
									<div
										key={`${section.id}-${subTab.id}`}
										className='min-w-0 max-w-full'
									>
										{subTab.component && (
											<subTab.component stackDetails={stack} />
										)}
									</div>
								),
						),
					)}
				</div>
			</div>
		</div>
	)
}

export default StackConfigPage
