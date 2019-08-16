import pytextrank, json, uuid, os, shutil

def retrieveSentences(content, word_limit):
    currpath = os.getcwd()
    folder = os.path.join(currpath, str(uuid.uuid4()))
    os.mkdir(folder)
    fname = str(uuid.uuid4())
    with open("{0}/{1}.json".format(folder, fname), "w") as f:
        f.write(json.dumps({"id": fname, "text": content}))
        f.close()
    path_stage0 = "{0}/{1}.json".format(folder, fname)
    path_stage1 = "{0}/o1.json".format(folder)
    with open(path_stage1, 'w') as f:
        for graf in pytextrank.parse_doc(pytextrank.json_iter(path_stage0)):
            f.write("%s\n" % pytextrank.pretty_print(graf._asdict()))
        f.close()
    path_stage2 = "{0}/o2.json".format(folder)
    graph, ranks = pytextrank.text_rank(path_stage1)
    #pytextrank.render_ranks(graph, ranks)
    with open(path_stage2, 'w') as f:
        for rl in pytextrank.normalize_key_phrases(path_stage1, ranks):
            f.write("%s\n" % pytextrank.pretty_print(rl._asdict()))
        f.close()
    kernel = pytextrank.rank_kernel(path_stage2)
    path_stage3 = "{0}/o3.json".format(folder)
    with open(path_stage3, 'w') as f:
        for s in pytextrank.top_sentences(kernel, path_stage1):
            f.write(pytextrank.pretty_print(s._asdict()))
            f.write("\n")
        f.close()
    sent_iter = sorted(pytextrank.limit_sentences(path_stage3, word_limit=word_limit), key=lambda x: x[1])
    s = []
    for sent_text, idx in sent_iter:
        s.append(pytextrank.make_sentence(sent_text))
    graf_text = " ".join(s)
    shutil.rmtree(folder)
    return s