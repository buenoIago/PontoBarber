import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { violacoesCriticas } from '../../../testing/axe';
import { type Servico } from '../servicos.models';
import { ServicosService } from '../servicos.service';
import { CatalogoServicos } from './catalogo';

const servico: Servico = {
  id: 's1',
  barbeariaId: 'b1',
  nome: 'Corte',
  duracaoMinutos: 30,
  preco: 50,
  ativo: true,
  ehModelo: false,
};

describe('CatalogoServicos', () => {
  let fixture: ComponentFixture<CatalogoServicos>;

  async function criar(tipo: string, codigoConvite: string | null): Promise<HTMLElement> {
    const barbearia = signal({ id: 'b1', codigoConvite });
    await TestBed.configureTestingModule({
      imports: [CatalogoServicos],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: ServicosService,
          useValue: {
            servicos: signal([servico]),
            carregando: signal(false),
            erro: signal(null),
            listar: vi.fn(),
            criar: vi.fn(),
            atualizar: vi.fn(),
            remover: vi.fn(),
            limparErro: vi.fn(),
          },
        },
        {
          provide: BarbeariaService,
          useValue: {
            barbeariaId: signal('b1'),
            barbearia,
            carregando: signal(false),
            erro: signal(null),
            selecionar: vi.fn(),
          },
        },
        {
          provide: AuthService,
          useValue: { conta: signal({ tipo }), erro: signal(null), limparErro: vi.fn() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CatalogoServicos);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should not offer management actions to customers', async () => {
    const elemento = await criar('Customer', null);

    expect(elemento.textContent).toContain('Corte');
    expect(elemento.querySelector('table')).not.toBeNull();
    expect(elemento.textContent).not.toContain('Novo serviço');
    expect(elemento.textContent).not.toContain('Editar');
    expect(elemento.textContent).not.toContain('Remover');
  });

  it('should offer full management to the owner', async () => {
    const elemento = await criar('Professional', 'ABC23456');

    expect(elemento.textContent).toContain('Novo serviço');
    expect(elemento.textContent).toContain('Editar');
    expect(elemento.textContent).toContain('Remover');
  });

  it('should offer only value editing to the barber', async () => {
    const elemento = await criar('Professional', null);

    expect(elemento.textContent).not.toContain('Novo serviço');
    expect(elemento.textContent).not.toContain('Remover');
    expect(elemento.textContent).toContain('Editar valores');
  });

  it('should select a barbearia only when an id is typed', async () => {
    await criar('Professional', 'ABC23456');
    const componente = fixture.componentInstance;
    const selecionar = vi.mocked(TestBed.inject(BarbeariaService).selecionar);

    componente.idInformado.set('  ');
    componente.selecionar();
    expect(selecionar).not.toHaveBeenCalled();

    componente.idInformado.set(' b2 ');
    componente.selecionar();
    expect(selecionar).toHaveBeenCalledWith('b2');
  });

  it('should reload the catalog when requested', async () => {
    await criar('Professional', 'ABC23456');
    const listar = vi.mocked(TestBed.inject(ServicosService).listar);
    listar.mockClear();

    fixture.componentInstance.recarregar();
    expect(listar).toHaveBeenCalledWith('b1');
  });

  it('should start and cancel creation resetting the form', async () => {
    await criar('Professional', 'ABC23456');
    const componente = fixture.componentInstance;

    componente.iniciarCriacao();
    expect(componente.criando()).toBe(true);

    componente.nome.set('Temporário');
    componente.cancelar();
    expect(componente.criando()).toBe(false);
    expect(componente.nome()).toBe('');
  });

  it('should fill the form when editing an existing service', async () => {
    await criar('Professional', 'ABC23456');
    const componente = fixture.componentInstance;

    componente.iniciarEdicao({ ...servico, preco: null, ativo: false });
    expect(componente.editandoId()).toBe('s1');
    expect(componente.nome()).toBe('Corte');
    expect(componente.duracao()).toBe(30);
    expect(componente.preco()).toBe(0);
    expect(componente.ativo()).toBe(false);
  });

  it('should reject invalid values before saving', async () => {
    await criar('Professional', 'ABC23456');
    const componente = fixture.componentInstance;
    const criarServico = vi.mocked(TestBed.inject(ServicosService).criar);

    componente.iniciarCriacao();
    componente.nome.set('Corte');
    componente.duracao.set(0);
    componente.preco.set(10);
    componente.salvar();
    expect(criarServico).not.toHaveBeenCalled();

    componente.duracao.set(30);
    componente.preco.set(-1);
    componente.salvar();
    expect(criarServico).not.toHaveBeenCalled();
  });

  it('should save through create and update paths', async () => {
    await criar('Professional', 'ABC23456');
    const componente = fixture.componentInstance;
    const servicos = TestBed.inject(ServicosService);
    const criarServico = vi.mocked(servicos.criar);
    const atualizar = vi.mocked(servicos.atualizar);

    componente.iniciarCriacao();
    componente.nome.set('Corte');
    componente.duracao.set(30);
    componente.preco.set(50);
    componente.salvar();
    expect(criarServico).toHaveBeenCalledWith(
      'b1',
      { nome: 'Corte', duracaoMinutos: 30, preco: 50, ativo: true },
      expect.any(Function),
    );

    componente.iniciarEdicao(servico);
    componente.ativo.set(false);
    componente.salvar();
    expect(atualizar).toHaveBeenCalledWith(
      'b1',
      's1',
      { nome: 'Corte', duracaoMinutos: 30, preco: 50, ativo: false },
      expect.any(Function),
    );

    const concluido = criarServico.mock.calls[0][2];
    concluido?.();
    expect(componente.sucesso()).toContain('salvo');
  });

  it('should save only editable values for the barber', async () => {
    await criar('Professional', null);
    const componente = fixture.componentInstance;
    const atualizar = vi.mocked(TestBed.inject(ServicosService).atualizar);

    componente.iniciarEdicao(servico);
    componente.duracao.set(45);
    componente.preco.set(60);
    componente.salvar();

    expect(atualizar).toHaveBeenCalledWith(
      'b1',
      's1',
      { duracaoMinutos: 45, preco: 60 },
      expect.any(Function),
    );
  });

  it('should remove a service and format prices', async () => {
    await criar('Professional', 'ABC23456');
    const componente = fixture.componentInstance;
    const remover = vi.mocked(TestBed.inject(ServicosService).remover);

    expect(componente.precoTexto(servico)).toBe('R$ 50.00');
    expect(componente.precoTexto({ ...servico, preco: null })).toBe('Não definido');

    componente.remover(servico);
    expect(remover).toHaveBeenCalledWith('b1', 's1', expect.any(Function));
    const concluido = remover.mock.calls[0][2];
    concluido?.();
    expect(componente.sucesso()).toContain('removido');
  });

  it('should not have critical axe violations', async () => {
    expect(await violacoesCriticas(await criar('Professional', 'ABC23456'))).toEqual([]);
  });
});
