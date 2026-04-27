export interface AuditEntry {
  id: number;
  entityName: string;
  entityId: string;
  action: string;
  changesJson: string;
  createdAtUtc: Date;
}
