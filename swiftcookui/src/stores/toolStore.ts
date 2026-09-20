import { defineStore } from 'pinia';
import api from '@/services/api';
import type { ToolDto, ToolCreateDto } from '@/interfaces/tool';
import { getErrorMessage } from '@/utils/errors';

export const useToolStore = defineStore('tool', {
  state: () => ({
    tools: [] as ToolDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
		this.loading = true
      try {
        const response = await api.get<ToolDto[]>('/tool');
        this.tools = response.data;
      } catch (err) {
        console.error('Failed to fetch tools:', err);
      } finally {
		  this.loading = false
	  }
    },
	
	async createTool(tool: ToolCreateDto) {
		try {
			const response = await api.post<ToolDto>('/tool/post', tool);
			this.tools.push(response.data)
			return response.data
		}
		catch (err: unknown)
		{
			this.error = getErrorMessage(err, 'Failed to create tool')
			throw err
		}
	}
  }
});
