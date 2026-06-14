import axios, { type AxiosRequestConfig } from 'axios'

export const http = axios.create({
  baseURL: '/',
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  },
})

export const apiClient = <T>(config: AxiosRequestConfig, options?: AxiosRequestConfig): Promise<T> => {
  return http({ ...config, ...options }).then(({ data }) => data as T)
}
