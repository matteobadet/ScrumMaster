import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from './apiClient';

/**
 * Construit une connexion SignalR vers le hub du board (contracts/realtime-hub.md).
 * La reconnexion automatique est activée ; l'appelant doit réémettre JoinBoard
 * et resynchroniser l'état via GET /api/boards/{boardId} sur l'événement `reconnected`.
 */
export function createRetroBoardConnection(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/retro-board`)
    .withAutomaticReconnect()
    .build();
}
