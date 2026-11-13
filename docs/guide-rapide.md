# Guide rapide – Projet VR « Premiers Gestes »

**Une question ? Demande à Emile.**

## 1. Installer (une seule fois)

1. Accepte l'invitation GitHub reçue par courriel.
2. Installe **GitHub Desktop** et connecte-toi avec ton compte.
3. Dans **Unity Hub**, installe **Unity 6000.6.2f1** avec le module **Android Build Support**.

## 2. Télécharger le projet (une seule fois)

1. GitHub Desktop → **File** → **Clone repository** → **Premier_geste** → **Clone**
2. Unity Hub → **Add** → **Add project from disk** → choisis le dossier du projet

## 3. Travailler (à chaque fois)

1. **Récupérer** : choisis **main** → **Fetch origin** → **Pull origin**
2. **Créer ta branche** : **Current branch** → **New branch** → par exemple `feat/6-telephone` (6 = le numéro de ta tâche)
3. **Travailler** dans Unity, dans **ta** scène `Sandbox_TonPrénom`
4. **Enregistrer** : écris par exemple « Ajoute le téléphone (#6) » → **Commit**
5. **Envoyer** : **Publish branch** (la 1re fois), puis **Push origin**
6. **Tâche finie** : **Create Pull Request** → écris `Closes #6` → préviens l'équipe pour qu'un membre approuve
7. **Après l'approbation** : **Squash and merge**, puis reviens sur **main** → **Pull origin**

## 4. Les 5 règles d'or

- ❌ Ne travaille **jamais** sur `main`.
- 🔄 **Pull** avant de commencer, **Push** à la fin de chaque séance.
- 🎯 **Une branche = une tâche.**
- 🏠 Travaille seulement dans **ta** scène Sandbox. Ne touche pas à la scène `Main`.
- 📝 Tout ce que tu télécharges va dans `ThirdParty`, et tu notes d'où ça vient. Chaque utilisation d'une IA doit aussi être notée.

## 5. Option : en ligne de commande

Pour ceux qui préfèrent le terminal à GitHub Desktop. Ce sont les mêmes étapes que la section 3.

**Une seule fois :** installe **Git** (git-scm.com, Git LFS est inclus), puis :

```text
git lfs install
git clone https://github.com/Projet-VR-Groupe5/Premier_geste.git
```

**À chaque tâche :**

```text
# 1. Récupérer
git checkout main
git pull

# 2. Créer ta branche
git checkout -b feat/6-telephone

# 3. Travailler dans Unity, puis voir ce qui a changé
git status

# 4. Enregistrer (seulement les fichiers de ta tâche)
git add Assets/_Project
git commit -m "Ajoute le téléphone (#6)"

# 5. Envoyer
git push -u origin feat/6-telephone    # la 1re fois
git push                               # les fois suivantes

# 6. Tâche finie : sur GitHub → Compare & pull request → écris Closes #6

# 7. Après la fusion
git checkout main
git pull
```

> ⚠️ **Évite `git add .`** : vérifie avec `git status` et ajoute seulement les fichiers de ta tâche. Unity modifie parfois des fichiers de réglages tout seul.

> ⚠️ **N'utilise jamais `git push --force`.**

## En cas de problème

**Ne force rien.** Fais une capture d'écran et envoie-la dans le groupe.
