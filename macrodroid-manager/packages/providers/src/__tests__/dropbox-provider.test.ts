import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { Scenario } from '@macrodroid-manager/converter';

vi.mock('dropbox', () => {
  const mockDbx = {
    usersGetCurrentAccount: vi.fn(),
    filesUpload: vi.fn(),
    filesListFolder: vi.fn(),
    filesDownload: vi.fn(),
    filesDeleteV2: vi.fn(),
  };
  return { Dropbox: vi.fn(() => mockDbx) };
});

const { Dropbox } = await import('dropbox');
const { DropboxProvider } = await import('../dropbox-provider.js');

function getMockDbx() {
  return (Dropbox as ReturnType<typeof vi.fn>).mock.results[0]?.value as {
    usersGetCurrentAccount: ReturnType<typeof vi.fn>;
    filesUpload: ReturnType<typeof vi.fn>;
    filesListFolder: ReturnType<typeof vi.fn>;
    filesDownload: ReturnType<typeof vi.fn>;
    filesDeleteV2: ReturnType<typeof vi.fn>;
  };
}

const testScenario: Scenario = {
  apiVersion: 'macrodroid/v1',
  kind: 'Scenario',
  metadata: {
    id: 'test-scenario',
    name: 'Test',
    version: 2,
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
  },
  macros: [],
};

describe('DropboxProvider', () => {
  let provider: InstanceType<typeof DropboxProvider>;

  beforeEach(() => {
    vi.clearAllMocks();
    provider = new DropboxProvider({ accessToken: 'test-token', basePath: '/MacroDroidManager' });
  });

  it('name は "dropbox"', () => {
    expect(provider.name).toBe('dropbox');
  });

  describe('testConnection()', () => {
    it('アカウント取得成功で true を返す', async () => {
      getMockDbx().usersGetCurrentAccount.mockResolvedValue({});
      const result = await provider.testConnection();
      expect(result).toBe(true);
    });

    it('エラーで false を返す', async () => {
      getMockDbx().usersGetCurrentAccount.mockRejectedValue(new Error('Unauthorized'));
      const result = await provider.testConnection();
      expect(result).toBe(false);
    });
  });

  describe('deployScenario()', () => {
    it('成功時に success: true を返す', async () => {
      getMockDbx().filesUpload.mockResolvedValue({ result: {} });
      const result = await provider.deployScenario(testScenario, Buffer.from('data'));
      expect(result.success).toBe(true);
      expect(result.provider).toBe('dropbox');
      expect(result.deployedAt).toBeDefined();
    });

    it('正しいパスにアップロードする', async () => {
      getMockDbx().filesUpload.mockResolvedValue({ result: {} });
      await provider.deployScenario(testScenario, Buffer.from('data'));
      const call = getMockDbx().filesUpload.mock.calls[0]![0] as { path: string };
      expect(call.path).toBe('/MacroDroidManager/deploy/scenarios/test-scenario_v2.mdr');
    });

    it('エラー時に success: false と error を返す', async () => {
      getMockDbx().filesUpload.mockRejectedValue(new Error('Network error'));
      const result = await provider.deployScenario(testScenario, Buffer.from('data'));
      expect(result.success).toBe(false);
      expect(result.error).toContain('Network error');
    });
  });

  describe('deployScript()', () => {
    it('成功時に success: true を返す', async () => {
      getMockDbx().filesUpload.mockResolvedValue({ result: {} });
      const result = await provider.deployScript('run.sh', '#!/bin/bash\necho hi');
      expect(result.success).toBe(true);
    });

    it('正しいパスにスクリプトをアップロードする', async () => {
      getMockDbx().filesUpload.mockResolvedValue({ result: {} });
      await provider.deployScript('run.sh', 'content');
      const call = getMockDbx().filesUpload.mock.calls[0]![0] as { path: string };
      expect(call.path).toBe('/MacroDroidManager/deploy/scripts/run.sh');
    });
  });

  describe('listExports()', () => {
    it('.mdr ファイルのみを返す', async () => {
      getMockDbx().filesListFolder.mockResolvedValue({
        result: {
          entries: [
            { '.tag': 'file', name: 'macro1.mdr', id: 'id1', server_modified: '2024-01-01', size: 1024 },
            { '.tag': 'file', name: 'readme.txt', id: 'id2', server_modified: '2024-01-01', size: 100 },
            { '.tag': 'folder', name: 'subfolder', id: 'id3' },
          ],
        },
      });
      const entries = await provider.listExports();
      expect(entries).toHaveLength(1);
      expect(entries[0]!.filename).toBe('macro1.mdr');
      expect(entries[0]!.sizeBytes).toBe(1024);
    });

    it('エラー時に空配列を返す', async () => {
      getMockDbx().filesListFolder.mockRejectedValue(new Error('Not found'));
      const entries = await provider.listExports();
      expect(entries).toEqual([]);
    });
  });

  describe('fetchExport()', () => {
    it('Buffer を返す', async () => {
      const data = new ArrayBuffer(8);
      getMockDbx().filesDownload.mockResolvedValue({ result: { fileBinary: data } });
      const buf = await provider.fetchExport({ id: 'id1', filename: 'macro1.mdr', uploadedAt: '', sizeBytes: 8 });
      expect(buf).toBeInstanceOf(Buffer);
      expect(buf.byteLength).toBe(8);
    });
  });

  describe('deleteExport()', () => {
    it('ファイルを削除する', async () => {
      getMockDbx().filesDeleteV2.mockResolvedValue({ result: {} });
      await provider.deleteExport({ id: 'id1', filename: 'macro1.mdr', uploadedAt: '', sizeBytes: 0 });
      expect(getMockDbx().filesDeleteV2).toHaveBeenCalledWith({
        path: '/MacroDroidManager/export/macro1.mdr',
      });
    });
  });

  it('basePath 末尾のスラッシュを除去する', async () => {
    vi.clearAllMocks();
    const p = new DropboxProvider({ accessToken: 'tok', basePath: '/MacroDroidManager/' });
    getMockDbx().filesUpload.mockResolvedValue({ result: {} });
    await p.deployScript('run.sh', 'x');
    const call = getMockDbx().filesUpload.mock.calls[0]![0] as { path: string };
    expect(call.path).toBe('/MacroDroidManager/deploy/scripts/run.sh');
  });
});
