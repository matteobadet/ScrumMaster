using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Data;

/// <summary>
/// Implémente <see cref="IDataProtectionKeyContext"/> pour persister l'anneau de clés Data
/// Protection (chiffrement du PAT Azure DevOps, specs/005-azure-devops-boards/research.md#2).
/// </summary>
public class ScrumMasterDbContext(DbContextOptions<ScrumMasterDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Equipe> Equipes => Set<Equipe>();

    public DbSet<Theme> Themes => Set<Theme>();

    public DbSet<Colonne> Colonnes => Set<Colonne>();

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<PostIt> PostIts => Set<PostIt>();

    public DbSet<Vote> Votes => Set<Vote>();

    public DbSet<PollUtilite> PollsUtilite => Set<PollUtilite>();

    public DbSet<VoteUtilite> VotesUtilite => Set<VoteUtilite>();

    public DbSet<RappelEnvoye> RappelsEnvoyes => Set<RappelEnvoye>();

    public DbSet<ConfigurationAzureDevOps> ConfigurationsAzureDevOps => Set<ConfigurationAzureDevOps>();

    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipe>(entity =>
        {
            entity.HasKey(e => e.AreaPath);
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).IsRequired();
            entity
                .HasMany(e => e.Colonnes)
                .WithOne(c => c.Theme)
                .HasForeignKey(c => c.ThemeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Colonne>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Intitule).IsRequired();
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AreaPath).IsRequired();
            entity.Property(e => e.Iteration).IsRequired();
            entity
                .HasOne(e => e.Equipe)
                .WithMany(eq => eq.Boards)
                .HasForeignKey(e => e.AreaPath)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(e => e.Theme)
                .WithMany()
                .HasForeignKey(e => e.ThemeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomAffiche).IsRequired();
            entity
                .HasOne(e => e.Board)
                .WithMany(b => b.Participants)
                .HasForeignKey(e => e.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostIt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Texte).IsRequired();
            entity
                .HasOne(e => e.Board)
                .WithMany(b => b.PostIts)
                .HasForeignKey(e => e.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Colonne)
                .WithMany()
                .HasForeignKey(e => e.ColonneId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(e => e.Auteur)
                .WithMany()
                .HasForeignKey(e => e.AuteurParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => new { e.PostItId, e.ParticipantId });
            entity
                .HasOne(e => e.PostIt)
                .WithMany(p => p.Votes)
                .HasForeignKey(e => e.PostItId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Participant)
                .WithMany()
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PollUtilite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity
                .HasOne(e => e.Equipe)
                .WithMany()
                .HasForeignKey(e => e.AreaPath)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.AreaPath, e.TypeReunion, e.Date }).IsUnique();
        });

        modelBuilder.Entity<VoteUtilite>(entity =>
        {
            entity.HasKey(e => new { e.PollId, e.TeamsUserId });
            entity.Property(e => e.NomAffiche).IsRequired();
            entity
                .HasOne(e => e.Poll)
                .WithMany(p => p.Votes)
                .HasForeignKey(e => e.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RappelEnvoye>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity
                .HasOne(e => e.Equipe)
                .WithMany()
                .HasForeignKey(e => e.AreaPath)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.AreaPath, e.TypeReunion, e.Date }).IsUnique();
        });

        modelBuilder.Entity<ConfigurationAzureDevOps>(entity =>
        {
            entity.HasKey(e => e.AreaPath);
            entity.Property(e => e.Organisation).IsRequired();
            entity.Property(e => e.Projet).IsRequired();
            entity.Property(e => e.PatChiffre).IsRequired();
            entity
                .HasOne(e => e.Equipe)
                .WithOne()
                .HasForeignKey<ConfigurationAzureDevOps>(e => e.AreaPath)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
