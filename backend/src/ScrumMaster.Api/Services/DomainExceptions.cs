namespace ScrumMaster.Api.Services;

/// <summary>Entrée invalide au sens métier (FR-015, champs obligatoires) — correspond à un 400.</summary>
public class DomainValidationException(string message) : Exception(message);

/// <summary>Ressource référencée introuvable — correspond à un 404.</summary>
public class DomainNotFoundException(string message) : Exception(message);

/// <summary>Action refusée pour l'appelant (ex : non-auteur, non-facilitateur) — correspond à un 403.</summary>
public class DomainForbiddenException(string message) : Exception(message);
