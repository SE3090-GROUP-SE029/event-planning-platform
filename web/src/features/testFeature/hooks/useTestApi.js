// import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
// import { testApi } from "../api/testApi";

// export function usePing() {
//     return useQuery({
//         queryKey: ['test', 'ping'],
//         queryFn: testApi.ping,
//         enabled: false,
//     });
// }

// export function useCreateMessage() {
//     const queryClient = useQueryClient();

//     return useMutation({
//         mutationFn: testApi.createMessage,

//         onSuccess: () => {
//             queryClient.invalidateQueries({
//                 queryKey: ['test', 'message'],
//             });
//         },
//     });
// }

// export function useGetMessage(id) {
//     return useQuery({
//         queryKey: ['test', 'message', id],
//         queryFn: () => testApi.getMessage(id),
//         enabled: !!id,
//     });
// }