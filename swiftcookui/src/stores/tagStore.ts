import { defineStore } from 'pinia';
import api from '@/services/api';
import type { TagDto, TagCreateDto } from '@/interfaces/tag';
import { getErrorMessage } from '@/utils/errors';

export const useTagStore = defineStore('tag', {
  state: () => ({
    tags: [] as TagDto[],
    error: null as string | null,
    loading: false,
  }),

  actions: {
    async fetchAll() {
		this.loading = true
      try {
        const response = await api.get<TagDto[]>('/tag');
        this.tags = response.data;
      } catch (err) {
        console.error('Failed to fetch tags:', err);
      } finally {
		  this.loading = false
	  }
    },
	
	async createTag(tag: TagCreateDto) {
		try {
			const response = await api.post<TagDto>('/tag/post', tag);
			this.tags.push(response.data)
			return response.data
		}
		catch (err: unknown)
		{
			this.error = getErrorMessage(err, 'Failed to create tag')
			throw err
		}
	}
  }
});
