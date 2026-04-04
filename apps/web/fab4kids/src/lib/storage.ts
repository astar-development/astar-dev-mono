import { BlobSASPermissions, BlobServiceClient } from '@azure/storage-blob';
import { SIGNED_URL_TTL_MINUTES } from '@/lib/constants.ts';

export async function generateSignedUrl(blobPath: string): Promise<string> {
  const connectionString = import.meta.env.AZURE_STORAGE_CONNECTION_STRING;
  const container = import.meta.env.AZURE_STORAGE_CONTAINER;

  const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
  const containerClient = blobServiceClient.getContainerClient(container);
  const blobClient = containerClient.getBlockBlobClient(blobPath);

  const expiresOn = new Date(Date.now() + SIGNED_URL_TTL_MINUTES * 60 * 1000);

  return blobClient.generateSasUrl({
    permissions: BlobSASPermissions.parse('r'),
    expiresOn,
  });
}
